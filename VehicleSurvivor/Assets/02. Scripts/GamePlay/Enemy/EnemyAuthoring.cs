using UnityEngine;
using Unity.Entities;

public class EnemyAuthoring : MonoBehaviour
{
    public float moveSpeed = 3f;

    class EnemyBaker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            // EnemySpeedData를 엔티티에 추가
            AddComponent(entity, new EnemySpeedData { Value = authoring.moveSpeed });
        }
    }
}