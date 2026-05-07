using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;

// 1. [핵심] 위치뿐만 아니라 "누구인지(Entity ID)"도 함께 저장하는 구조체 생성
public struct SpatialEnemyData
{
    public Entity EnemyEntity;
    public float3 Position;
}

[BurstCompile]
[UpdateAfter(typeof(EnemyMovementSystem))]
public partial struct EnemySeparationSystem : ISystem
{
    private const float CellSize = 3.0f;

    // 저장 방식을 SpatialEnemyData로 변경
    private NativeParallelMultiHashMap<int, SpatialEnemyData> m_SpatialMap;
    private EntityQuery m_EnemyQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // 2. [핵심] EnemyNavData가 아닌, 현재 사용 중인 NavAgentComponent를 찾도록 수정!
        state.RequireForUpdate<NavAgentComponent>();
        m_EnemyQuery = SystemAPI.QueryBuilder().WithAll<NavAgentComponent, LocalTransform>().Build();

        m_SpatialMap = new NativeParallelMultiHashMap<int, SpatialEnemyData>(10000, Allocator.Persistent);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        int enemyCount = m_EnemyQuery.CalculateEntityCount();

        // 이제 컴포넌트를 제대로 찾으므로 0이 나오지 않고 정상 실행됩니다!
        if (enemyCount == 0) return;

        if (m_SpatialMap.Capacity < enemyCount)
        {
            m_SpatialMap.Capacity = enemyCount + 1000;
        }
        m_SpatialMap.Clear();

        var fillJob = new FillSpatialMapJob
        {
            CellSize = CellSize,
            SpatialMap = m_SpatialMap.AsParallelWriter()
        };
        var fillHandle = fillJob.ScheduleParallel(state.Dependency);

        var separateJob = new EnemySeparationJob
        {
            CellSize = CellSize,
            DeltaTime = SystemAPI.Time.DeltaTime,
            SpatialMap = m_SpatialMap
        };

        state.Dependency = separateJob.ScheduleParallel(fillHandle);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (m_SpatialMap.IsCreated)
        {
            m_SpatialMap.Dispose();
        }
    }
}

[BurstCompile]
public partial struct FillSpatialMapJob : IJobEntity
{
    public float CellSize;
    public NativeParallelMultiHashMap<int, SpatialEnemyData>.ParallelWriter SpatialMap;

    // 파라미터에 'Entity entity'를 추가하여 자신의 고유 ID를 받아옵니다.
    private void Execute(Entity entity, in LocalTransform transform, in NavAgentComponent navAgent)
    {
        int hash = GetCellHash(transform.Position, CellSize);
        SpatialMap.Add(hash, new SpatialEnemyData { EnemyEntity = entity, Position = transform.Position });
    }

    public static int GetCellHash(float3 pos, float cellSize)
    {
        int x = (int)math.floor(pos.x / cellSize);
        int z = (int)math.floor(pos.z / cellSize);
        return (x * 73856093) ^ (z * 19349663);
    }
}

[BurstCompile]
public partial struct EnemySeparationJob : IJobEntity
{
    public float CellSize;
    public float DeltaTime;

    [ReadOnly]
    public NativeParallelMultiHashMap<int, SpatialEnemyData> SpatialMap;

    // 여기도 'Entity entity' 파라미터 추가
    private void Execute(Entity entity, ref LocalTransform transform, in NavAgentComponent navAgent)
    {
        float3 myPos = transform.Position;
        int cx = (int)math.floor(myPos.x / CellSize);
        int cz = (int)math.floor(myPos.z / CellSize);

        float3 separationVector = float3.zero;

        float avoidRadius = 2.5f; // 자동차 크기에 맞게 2.5f로 롤백
        float avoidRadiusSq = avoidRadius * avoidRadius;

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                int hash = ((cx + x) * 73856093) ^ ((cz + z) * 19349663);

                if (SpatialMap.TryGetFirstValue(hash, out SpatialEnemyData otherData, out var it))
                {
                    do
                    {
                        // 3. [핵심] 지도를 뒤지다가 '나 자신'을 발견하면 밀어내지 않고 패스!
                        if (otherData.EnemyEntity == entity) continue;

                        float3 diff = myPos - otherData.Position;
                        diff.y = 0;
                        float distSq = math.lengthsq(diff);

                        // 남인데 나랑 완벽하게 좌표가 겹쳤을 때 (Zero Distance 버그 해결)
                        if (distSq < 0.001f)
                        {
                            // 각자의 고유 인덱스를 이용해 서로 다른 방향으로 강제 분리
                            diff = new float3(math.sin(entity.Index), 0, math.cos(entity.Index));
                            distSq = 0.01f;
                        }

                        if (distSq < avoidRadiusSq)
                        {
                            float dist = math.sqrt(distSq);
                            float pushStrength = (avoidRadius - dist) / avoidRadius;
                            separationVector += (diff / dist) * pushStrength;
                        }
                    } while (SpatialMap.TryGetNextValue(out otherData, ref it));
                }
            }
        }

        if (math.lengthsq(separationVector) > 0.001f)
        {
            // 수치 정상화: 2000은 차가 우주로 날아갈 수 있으니 50.0f 정도로 설정합니다.
            transform.Position += separationVector * DeltaTime * 10.0f;
        }
    }
}