using UnityEngine;
using System.Collections;

public class StageClearObject : MonoBehaviour
{
    //[Header("Stage References")]
    //[Tooltip("클리어 후 닫아버릴 입구 (예: 투명한 차단벽 오브젝트)")]
    //public GameObject stageEntranceBlocker;

    //[Tooltip("클리어 후 아예 꺼버릴 스테이지 전체 맵(환경)의 최상위 부모")]
    //public GameObject stageEnvironment;

    [Tooltip("광장으로 돌아갈 스폰 포인트")]
    public Transform hubRespawnPoint;

    [Header("Effects & Timing")]
    [Tooltip("열쇠 획득 시 터질 팡파레/폭발 이펙트")]
    public GameObject pickupVFX;

    [Tooltip("획득 후 마을로 텔레포트되기까지의 대기 시간(초)")]
    public float returnDelay = 1.0f;

    private bool isCollected = false;

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어가 닿았고, 아직 먹지 않은 상태일 때만 발동
        if (!isCollected && other.CompareTag("Player"))
        {
            StartCoroutine(KeyPickupRoutine(other.gameObject));
        }
    }

    private IEnumerator KeyPickupRoutine(GameObject player)
    {
        isCollected = true; // 중복 획득 방지

        // 1. 이펙트 생성 (열쇠 위치에 팡!)
        if (pickupVFX != null)
        {
            Instantiate(pickupVFX, transform.position, Quaternion.identity);
        }

        // 2. 열쇠 모델링과 충돌체를 즉시 숨깁니다. (먹은 것처럼 보이게 연출)
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }

        // 3. 설정한 시간(returnDelay) 동안 대기하며 이펙트 감상
        yield return new WaitForSeconds(returnDelay);

        // 4. 게임 매니저에 스테이지 클리어 신호 전송 (미드 보스 소환 등)
        if (StageRunManager.Instance != null)
        {
            StageRunManager.Instance.OnStageCleared();
        }

        // 5. 플레이어를 광장으로 복귀시킵니다.
        PlayerRespawn playerRespawn = player.GetComponent<PlayerRespawn>();
        if (playerRespawn != null)
        {
            // 클리어했으므로 낙사 시 부활할 위치를 다시 '광장'으로 갱신!
            if (hubRespawnPoint != null)
            {
                playerRespawn.currentRespawnPoint = hubRespawnPoint;
            }

            // PlayerRespawn의 텔레포트 로직을 활용해 광장으로 즉시 이동
            playerRespawn.Respawn();
        }

        //// 6. 해당 스테이지 입구 차단 및 맵 최상위 부모 비활성화 (최적화)
        //if (stageEntranceBlocker != null) stageEntranceBlocker.SetActive(true); // 막는 벽 켜기
        //if (stageEnvironment != null) stageEnvironment.SetActive(false);        // 전체 맵 끄기

        // 7. 역할이 끝난 열쇠 오브젝트 완전 삭제
        Destroy(gameObject);
    }
}