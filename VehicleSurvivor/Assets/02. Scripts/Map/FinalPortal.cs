using UnityEngine;

public class FinalPortal : MonoBehaviour
{
    [Tooltip("포탈이 생성될 때 약간의 딜레이를 주어, 플레이어가 실수로 바로 타버리는 것을 방지합니다.")]
    public float activeDelay = 1.0f;

    private bool isReady = false;

    private void Start()
    {
        // 1초 뒤에 포탈 탑승이 가능해지도록 설정
        Invoke(nameof(ActivatePortal), activeDelay);
    }

    private void ActivatePortal()
    {
        isReady = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isReady) return;

        if (other.CompareTag("Player"))
        {
            SoundPlayback.PlaySfx(ESoundId.SFX_Env_Portal);
            Debug.Log("플레이어가 최종 보스 포탈에 탑승했습니다!");

            if (StageRunManager.Instance != null)
            {
                // 스테이지 매니저에게 플레이어를 넘기며 맵 이동 실행
                StageRunManager.Instance.TransitionToFinalBoss(other.gameObject);
            }

            // 포탈은 역할이 끝났으니 삭제
            Destroy(gameObject);
        }
    }
}