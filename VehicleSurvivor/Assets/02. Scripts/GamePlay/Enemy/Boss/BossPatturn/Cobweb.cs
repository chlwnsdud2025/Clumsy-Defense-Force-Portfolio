using UnityEngine;
using DG.Tweening; // DOTween을 쓰기 위해 반드시 필요합니다!

public class Cobweb : MonoBehaviour
{
    [Header("Cobweb Settings")]
    [Tooltip("거미줄 장판이 유지되다가 자동으로 사라질 시간(초)입니다.")]
    public float duration = 5.0f;

    [Tooltip("거미줄에 닿았을 때 감소시킬 이동속도 비율 (0.4 = 원래 속도의 40%로 저하)")]
    [Range(0.1f, 0.9f)]
    public float speedMultiplier = 0.4f;

    [Header("DOTween Animation Settings")]
    [Tooltip("거미줄이 생성되어 목표 크기까지 커지는 데 걸리는 시간(초)")]
    public float scaleUpDuration = 0.4f;

    [Tooltip("거미줄이 사라질 때 슥 줄어드는 데 걸리는 시간(초)")]
    public float scaleDownDuration = 0.3f;

    private Vector3 targetLocalScale;
    private bool isPlayerInside = false;
    private GameObject cachedPlayer = null;

    void Start()
    {
        // 1. 인스펙터 창에서 세팅해 둔 거미줄 프리팹 고유의 스케일 값을 백업합니다.
        targetLocalScale = transform.localScale;

        // 2. 요청하신 스폰 시 X축 -90도 회전 고정값 적용
        // (원래 오프셋 회전이 들어가 있다면 덧셈 방식으로 연산합니다.)
        transform.localRotation = Quaternion.Euler(-90f, transform.localRotation.eulerAngles.y, transform.localRotation.eulerAngles.z);

        // 3. 크기를 0으로 만든 뒤, DOTween으로 찰지게 키워줍니다. (Ease.OutBack을 쓰면 살짝 튀겨서 예쁩니다)
        transform.localScale = Vector3.zero;
        transform.DOScale(targetLocalScale, scaleUpDuration).SetEase(Ease.OutBack);

        // 4. 지정한 지속시간(duration)이 끝나기 직전에 축소 연출 후 파괴되도록 Invoke 예약
        Invoke(nameof(StartDisappearing), Mathf.Max(0.1f, duration - scaleDownDuration));
    }

    private void StartDisappearing()
    {
        // 사라질 때는 부드럽게 크기를 0으로 줄이고, 완전히 줄어들면 게임 오브젝트를 파괴합니다.
        transform.DOScale(Vector3.zero, scaleDownDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => {
                Destroy(gameObject);
            });
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isPlayerInside)
        {
            PlayerMotor motor = other.GetComponent<PlayerMotor>();
            PlayerController controller = other.GetComponent<PlayerController>();

            if (motor != null || controller != null)
            {
                cachedPlayer = other.gameObject;
                isPlayerInside = true;

                Debug.Log("<color=cyan>플레이어가 거미줄을 밟았습니다! (디버프 연산 TODO 작동)</color>");

                // ==========================================
                // TODO: 플레이어 스크립트를 건드리지 않고 속도를 줄이는 로직을 여기에 구현하세요.
                // 
                // [구현 힌트]
                // 방법 A. 팀원이 만든 PlayerMotor의 'MoveSpeed' 변수가 public이라면 아래처럼 강제 변조:
                //         motor.MoveSpeed *= speedMultiplier;
                // ==========================================
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && isPlayerInside)
        {
            ReleasePlayerDebuff();
        }
    }

    private void OnDestroy()
    {
        // DOTween 시퀀스가 끝나 오브젝트가 파괴되거나, 플레이어가 안에 있는 도중 거미줄이 터졌을 때 디버프 강제 해제
        if (isPlayerInside)
        {
            ReleasePlayerDebuff();
        }
    }

    private void ReleasePlayerDebuff()
    {
        if (cachedPlayer != null)
        {
            Debug.Log("<color=cyan>거미줄 영역 해제! (디버프 복구 TODO 작동)</color>");

            // ==========================================
            // TODO: 거미줄에서 벗어났을 때 플레이어의 속도를 원래대로 복구하는 로직을 구현하세요.
            // ==========================================
        }
        isPlayerInside = false;
        cachedPlayer = null;
    }
}