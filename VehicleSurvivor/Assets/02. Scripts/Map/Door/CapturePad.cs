using UnityEngine;
using UnityEngine.Events;
using TMPro; // TextMeshPro를 사용하기 위해 반드시 필요한 네임스페이스!

public class CapturePad : MonoBehaviour
{
    [Header("Capture Settings")]
    public float requiredTime = 3.0f; // 점령에 필요한 시간
    public bool resetOnLeave = false; // 나갔을 때 게이지 초기화 여부

    [Tooltip("쉐이더 색상 프로퍼티 이름 (URP 환경이면 _BaseColor, 일반 3D면 _Color)")]
    public string colorPropertyName = "_BaseColor"; // [수정] URP 기본값으로 변경

    [Header("UI Settings")]
    [Tooltip("점령 진행도를 표시할 3D TextMeshPro 오브젝트를 연결하세요.")]
    public TextMeshPro progressText;

    [Header("Events (점령 완료 시 실행할 작업들)")]
    [Tooltip("여기에 문 열기, 범퍼카 끄기, 점프대 켜기 등을 인스펙터에서 추가하세요.")]
    public UnityEvent onCaptureComplete;

    private float currentCaptureTime = 0f;
    private bool isCaptured = false;
    private Renderer padRenderer;

    // [최적화용 변수]
    private MaterialPropertyBlock propBlock;
    private int colorPropertyID;

    void Start()
    {
        padRenderer = GetComponent<Renderer>();

        // [최적화 세팅]
        propBlock = new MaterialPropertyBlock();
        colorPropertyID = Shader.PropertyToID(colorPropertyName);

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

            // [복구 완료!] 실시간으로 퍼센트 텍스트 업데이트
            UpdateProgressText(progress);

            // [최적화 적용] 매테리얼 복제 없이 색상만 부드럽게 변경!
            Color currentColor = Color.Lerp(Color.gray, Color.green, progress);
            if (padRenderer != null)
            {
                padRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor(colorPropertyID, currentColor);
                padRenderer.SetPropertyBlock(propBlock);
            }

            if (currentCaptureTime >= requiredTime)
            {
                isCaptured = true;

                if (padRenderer != null)
                {
                    padRenderer.GetPropertyBlock(propBlock);
                    propBlock.SetColor(colorPropertyID, Color.green);
                    padRenderer.SetPropertyBlock(propBlock);
                }

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

                if (padRenderer != null)
                {
                    padRenderer.GetPropertyBlock(propBlock);
                    propBlock.SetColor(colorPropertyID, Color.white);
                    padRenderer.SetPropertyBlock(propBlock);
                }

                
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
                progressText.text = "100%";
                progressText.color = Color.green; // 텍스트 색상도 초록색으로 짠! 하고 바꿔줍니다.
            }
            else
            {
                progressText.text = $"{percent}%";
            }
        }
    }
}