using Unity.Entities;
using UnityEngine;

public class PlayerTestScript : MonoBehaviour
{
    // 테스트용으로 비워둠 (이동 로직은 기존 PlayerController 참고)
}

public class PlayerBaker : Baker<PlayerTestScript>
{
    public override void Bake(PlayerTestScript authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        // 시스템이 찾을 수 있도록 PlayerTag 추가
        AddComponent<PlayerTag>(entity);
    }
}