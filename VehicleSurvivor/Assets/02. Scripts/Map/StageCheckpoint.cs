using UnityEngine;

public class StageCheckpoint : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("이 입구를 지나간 후 떨어지면 부활할 스폰 포인트")]
    public Transform myStageSpawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        // 몹은 레이어 매트릭스에 의해 이미 걸러지므로, 플레이어(혹은 투사체 등)만 여기로 들어옵니다.
        if (other.CompareTag("Player"))
        {
            PlayerRespawn respawn = other.GetComponent<PlayerRespawn>();
            if (respawn != null)
            {
                // 플레이어의 부활 위치를 이 스테이지의 스폰 포인트로 갱신합니다.
                respawn.currentRespawnPoint = myStageSpawnPoint;
            }
        }
    }
}