using UnityEngine;
using UnityEngine.Events;
using TMPro; // [핵심] TextMeshPro를 사용하기 위해 반드시 필요한 네임스페이스!

public class CapturePad : MonoBehaviour
{
    [Header("Capture Settings")]
    public float requiredTime = 3.0f; // 점령에 필요한 시간
    public bool resetOnLeave = false; // 나갔을 때 게이지 초기화 여부

    [Header("UI Settings")]
    [Tooltip("점령 진행도를 표시할 3D TextMeshPro 오브젝트를 연결하세요.")]
    public TextMeshPro progressText; // [추가] 퍼센트를 띄울 텍스트 변수

    [Header("Events (점령 완료 시 실행할 작업들)")]
    [Tooltip("여기에 문 열기, 범퍼카 끄기, 점프대 켜기 등을 인스펙터에서 추가하세요.")]
    public UnityEvent onCaptureComplete;

    private float currentCaptureTime = 0f;
    private bool isCaptured = false;
    private Renderer padRenderer;

    void Start()
    {
        padRenderer = GetComponent<Renderer>();

        // 게임 시작 시 0%로 초기화
        UpdateProgressText(0f);
    }

    private void OnTriggerStay(Collider other)
    {
        if (isCaptured) return;

        if (other.CompareTag("Player"))
        {
            currentCaptureTime += Time.deltaTime;

            // 점령 진행도 (0.0 ~ 1.0)
            float progress = currentCaptureTime / requiredTime;
            padRenderer.material.color = Color.Lerp(Color.gray, Color.green, progress);

            // [추가] 실시간으로 퍼센트 텍스트 업데이트
            UpdateProgressText(progress);

            if (currentCaptureTime >= requiredTime)
            {
                isCaptured = true;
                padRenderer.material.color = Color.green;

                // [추가] 점령 완료 시 무조건 100%로 텍스트 고정
                UpdateProgressText(1f);

                // 점령 완료 이벤트 실행!
                onCaptureComplete?.Invoke();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isCaptured) return;

        if (other.CompareTag("Player"))
        {
            if (resetOnLeave)
            {
                currentCaptureTime = 0f;
                padRenderer.material.color = Color.gray;

            }
        }
    }


    private void UpdateProgressText(float progress)
    {
        if (progressText != null)
        {
            // progress(예: 0.453)를 100을 곱해 45.3으로 만들고, 소수점을 버려서 깔끔한 45 정수로 만듭니다.
            int percent = Mathf.Clamp(Mathf.FloorToInt(progress * 100f), 0, 100);

            if (isCaptured)
            {
                // 점령이 끝났을 때는 100% 대신 "MAX"나 "완료!" 같은 텍스트를 띄우고 싶다면 여기를 수정하세요.
                progressText.text = "Clear!";
                progressText.color = Color.green; // 텍스트 색상도 초록색으로 짠! 하고 바꿔줍니다.
            }
            else
            {
                progressText.text = $"{percent}%";
            }
        }
    }
}