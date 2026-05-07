using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using UnityEngine.Experimental.AI;
using UnityEngine.AI;

[BurstCompile]
public partial struct EnemyMovementSystem : ISystem
{
    private NavMeshQuery m_Query;
    private bool m_IsQueryCreated;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerTag>();
        m_IsQueryCreated = false;
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<PlayerTag>(out Entity playerEntity)) return;
        float3 playerPos = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;

        float currentTime = (float)SystemAPI.Time.ElapsedTime;
        float deltaTime = SystemAPI.Time.DeltaTime;

        if (!m_IsQueryCreated)
        {
            // 전역에서 돌려쓸 단 1개의 쿼리만 안전하게 생성
            m_Query = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.Persistent, 1024);
            m_IsQueryCreated = true;
        }

        // --- 1. [Main Thread Burst] 길 찾기 연산 ---
        // 쿨타임이 된 극소수의 적들만 에러 없이 안전하게 길을 찾습니다. (Burst 적용으로 초고속)
        foreach (var (navAgent, transform, entity) in SystemAPI.Query<RefRW<NavAgentComponent>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            if (navAgent.ValueRO.nextPathCalculateTime < currentTime)
            {
                // 쿨타임 분산 (프레임 과부하 방지)
                navAgent.ValueRW.nextPathCalculateTime = currentTime + 1.0f + (math.frac(transform.ValueRO.Position.x) * 0.5f);
                navAgent.ValueRW.pathCalculated = false;

                DynamicBuffer<WaypointBuffer> waypointBuffer = state.EntityManager.GetBuffer<WaypointBuffer>(entity);

                // 안전한 메인 스레드 쿼리 실행
                CalculatePath(ref navAgent.ValueRW, transform.ValueRO.Position, ref waypointBuffer, playerPos);
            }
        }

        // --- 2. [Multi-Thread Job] 병렬 주행 ---
        // 길 찾기가 완료된 수만 마리의 적들을 멀티스레드로 렉 없이 동시에 움직입니다.
        var moveJob = new EnemyMoveParallelJob
        {
            DeltaTime = deltaTime,
            PlayerPosition = playerPos
        };
        state.Dependency = moveJob.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (m_IsQueryCreated)
        {
            m_Query.Dispose();
            m_IsQueryCreated = false;
        }
    }

    // 쿼리 에러가 절대 나지 않는 구조로 변경
    private void CalculatePath(ref NavAgentComponent navAgent, float3 currentPos, ref DynamicBuffer<WaypointBuffer> waypointBuffer, float3 targetPos)
    {
        float3 extents = new float3(5f, 5f, 5f);
        var startLoc = m_Query.MapLocation(currentPos, extents, 0);
        var endLoc = m_Query.MapLocation(targetPos, extents, 0);

        if (m_Query.IsValid(startLoc) && m_Query.IsValid(endLoc))
        {
            var status = m_Query.BeginFindPath(startLoc, endLoc, NavMesh.AllAreas);
            if (status == PathQueryStatus.InProgress || status == PathQueryStatus.Success)
            {
                status = m_Query.UpdateFindPath(1024, out int iterations);
                if (status == PathQueryStatus.Success)
                {
                    status = m_Query.EndFindPath(out int pathSize);
                    if (pathSize > 0)
                    {
                        waypointBuffer.Clear();
                        var polys = new NativeArray<PolygonId>(pathSize, Allocator.Temp);
                        m_Query.GetPathResult(polys);

                        for (int i = 0; i < pathSize - 1; i++)
                        {
                            if (m_Query.GetPortalPoints(polys[i], polys[i + 1], out UnityEngine.Vector3 left, out UnityEngine.Vector3 right))
                            {
                                float3 center = ((float3)left + (float3)right) * 0.5f;
                                waypointBuffer.Add(new WaypointBuffer { wayPoint = center });
                            }
                        }
                        waypointBuffer.Add(new WaypointBuffer { wayPoint = targetPos });

                        navAgent.currentWaypoint = 0;
                        navAgent.pathCalculated = true; // 이동 허가
                        polys.Dispose();
                    }
                }
            }
        }
    }
}

// ---------------------------------------------------------
// 수만 마리의 이동 처리를 전담하는 완벽한 멀티스레드 Job
// ---------------------------------------------------------
[BurstCompile]
public partial struct EnemyMoveParallelJob : IJobEntity
{
    public float DeltaTime;
    public float3 PlayerPosition;

    private void Execute(
        ref LocalTransform transform,
        ref NavAgentComponent navAgent,
        ref DynamicBuffer<WaypointBuffer> waypointBuffer)
    {
        // 아직 길이 없거나 버퍼가 비어있으면 대기
        if (!navAgent.pathCalculated || waypointBuffer.Length == 0) return;

        // 정지 거리 체크
        float distanceToPlayer = math.distance(transform.Position, PlayerPosition);
        if (distanceToPlayer < 2.0f)
        {
            float3 lookDir = math.normalizesafe(PlayerPosition - transform.Position);
            if (math.lengthsq(lookDir) > 0.001f)
            {
                transform.Rotation = math.slerp(transform.Rotation, quaternion.LookRotationSafe(lookDir, math.up()), DeltaTime * 5.0f);
            }
            return;
        }

        // 주행 로직
        float3 targetPos = waypointBuffer[navAgent.currentWaypoint].wayPoint;
        targetPos.y = transform.Position.y;

        if (math.distance(transform.Position, targetPos) < 1.0f)
        {
            if (navAgent.currentWaypoint + 1 < waypointBuffer.Length)
            {
                navAgent.currentWaypoint += 1;
                targetPos = waypointBuffer[navAgent.currentWaypoint].wayPoint;
                targetPos.y = transform.Position.y;
            }
        }

        float3 direction = targetPos - transform.Position;
        float3 desiredDirection = math.normalizesafe(direction);

        if (math.lengthsq(desiredDirection) > 0.001f)
        {
            transform.Rotation = math.slerp(transform.Rotation, quaternion.LookRotationSafe(desiredDirection, math.up()), DeltaTime * 5.0f);
        }

        transform.Position += desiredDirection * DeltaTime * navAgent.moveSpeed;
    }
}