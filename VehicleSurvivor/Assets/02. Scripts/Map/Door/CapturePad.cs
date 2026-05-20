using UnityEngine;
using UnityEngine.Events; // UnityEvent를 사용하기 위해 꼭 필요합니다!

public class CapturePad : MonoBehaviour
{
    [Header("Capture Settings")]
    public float requiredTime = 3.0f; // 점령에 필요한 시간
    public bool resetOnLeave = false; // 나갔을 때 게이지 초기화 여부

    [Header("Events (점령 완료 시 실행할 작업들)")]
    [Tooltip("여기에 문 열기, 범퍼카 끄기, 점프대 켜기 등을 인스펙터에서 추가하세요.")]
    public UnityEvent onCaptureComplete;

    private float currentCaptureTime = 0f;
    private bool isCaptured = false;
    private Renderer padRenderer;

    void Start()
    {
        padRenderer = GetComponent<Renderer>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (isCaptured) return;

        if (other.CompareTag("Player"))
        {
            currentCaptureTime += Time.deltaTime;

            // 점령 진행도에 따라 색상을 서서히 변경
            float progress = currentCaptureTime / requiredTime;
            padRenderer.material.color = Color.Lerp(Color.gray, Color.green, progress);

            if (currentCaptureTime >= requiredTime)
            {
                isCaptured = true;
                padRenderer.material.color = Color.green;

                // [핵심] 점령이 완료되면 인스펙터에 등록된 모든 작업(Event)을 동시에 실행합니다!
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
}