using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;

public struct EnemySpawnerData : IComponentData
{
    public Entity EnemyPrefab;
    public float SpawnInterval;
    public float Timer;
}

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 0.1f;

    class Baker : Baker<EnemySpawner>
    {
        public override void Bake(EnemySpawner authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EnemySpawnerData
            {
                EnemyPrefab = GetEntity(authoring.enemyPrefab, TransformUsageFlags.Dynamic),
                SpawnInterval = authoring.spawnInterval,
                Timer = 0f
            });
        }
    }
}
[BurstCompile]
public partial struct EnemySpawnerSystem : ISystem
{
    private uint seed;

    public void OnCreate(ref SystemState state)
    {
        seed = 1;
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 1. 안전한 생성을 위한 명령 일지(ECB) 생성
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        float deltaTime = SystemAPI.Time.DeltaTime;
        var random = Unity.Mathematics.Random.CreateFromIndex(seed++);

        float3 playerPos = float3.zero;
        bool isPlayerFound = false;

        foreach (var playerTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerTag>())
        {
            playerPos = playerTransform.ValueRO.Position;
            isPlayerFound = true;
            break;
        }

        if (!isPlayerFound) return;

        foreach (var spawner in SystemAPI.Query<RefRW<EnemySpawnerData>>())
        {
            spawner.ValueRW.Timer -= deltaTime;
            if (spawner.ValueRO.Timer <= 0f)
            {
                spawner.ValueRW.Timer = spawner.ValueRO.SpawnInterval;

                // 2. 중요: EntityManager.Instantiate 대신 ecb.Instantiate 사용
                Entity newEnemy = ecb.Instantiate(spawner.ValueRO.EnemyPrefab);

                float2 randomDir = random.NextFloat2Direction();
                float spawnRadius = 35f;
                float3 spawnPos = playerPos + new float3(randomDir.x, 0, randomDir.y) * spawnRadius;

                // 3. 중요: SetComponentData 대신 ecb.SetComponent 사용
                ecb.SetComponent(newEnemy, new LocalTransform
                {
                    Position = spawnPos,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
            }
        }
    }
}