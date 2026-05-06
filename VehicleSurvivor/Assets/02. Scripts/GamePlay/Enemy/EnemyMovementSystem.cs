using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;

[BurstCompile]
public partial struct EnemyMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float3 playerPos = float3.zero;

        // PlayerTag를 가진 엔티티의 위치를 실시간으로 찾음
        foreach (var playerTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerTag>())
        {
            playerPos = playerTransform.ValueRO.Position;
            break;
        }

        new EnemyMoveJob
        {
            DeltaTime = deltaTime,
            PlayerPosition = playerPos
        }.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct EnemyMoveJob : IJobEntity
{
    public float DeltaTime;
    public float3 PlayerPosition;

    private void Execute(ref LocalTransform transform, in EnemySpeedData speedData)
    {
        float3 direction = PlayerPosition - transform.Position;
        direction = math.normalizesafe(direction);
        transform.Position += direction * speedData.Value * DeltaTime;
    }
}