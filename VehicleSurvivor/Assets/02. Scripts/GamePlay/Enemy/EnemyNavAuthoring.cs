using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// 가이드 이미지 1: NavAgentComponent
public struct NavAgentComponent : IComponentData
{
    public bool pathCalculated;
    public int currentWaypoint;
    public float moveSpeed;
    public float nextPathCalculateTime;
}

// 가이드 이미지 1: WaypointBuffer
[InternalBufferCapacity(16)]
public struct WaypointBuffer : IBufferElementData
{
    public float3 wayPoint;
}

// 가이드 이미지 2: NavAgentAuthoring
public class EnemyNavAuthoring : MonoBehaviour
{
    public float moveSpeed = 5f; // 기본 이동 속도

    class Baker : Baker<EnemyNavAuthoring>
    {
        public override void Bake(EnemyNavAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new NavAgentComponent
            {
                pathCalculated = false,
                currentWaypoint = 0,
                moveSpeed = authoring.moveSpeed,
                nextPathCalculateTime = 0f
            });

            AddBuffer<WaypointBuffer>(entity);
        }
    }
}