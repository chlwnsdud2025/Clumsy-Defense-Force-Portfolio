using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class CapturePad : MonoBehaviour
{
    [Header("Capture Settings")]
    public float requiredTime = 3.0f;
    public bool resetOnLeave = false;

    public string colorPropertyName = "_BaseColor";

    [Header("UI Settings")]
    public TextMeshPro progressText;

    [Header("Events (점령 완료 시 실행할 작업들)")]
    public UnityEvent onCaptureComplete;

    private float currentCaptureTime = 0f;
    private bool isCaptured = false;
    private Renderer padRenderer;

    private MaterialPropertyBlock propBlock;
    private int colorPropertyID;

    // 원래 텍스트 색상을 기억해둘 변수
    private Color originalTextColor = Color.white;

    void Start()
    {
        padRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
        colorPropertyID = Shader.PropertyToID(colorPropertyName);

        if (progressText != null)
        {
            originalTextColor = progressText.color;
        }

        UpdateProgressText(0f);
    }

    private void OnTriggerStay(Collider other)
    {
        if (isCaptured) return;

        if (other.CompareTag("Player"))
        {
            // ==========================================
            // 🔒 [핵심 방어] 보스가 살아있으면 점령 게이지 상승 차단!
            // ==========================================
            if (StageRunManager.Instance != null && StageRunManager.Instance.isBossActive)
            {
                if (progressText != null)
                {
                    progressText.text = "보스 처치 필요!";
                    progressText.color = Color.red; // 시각적 경고
                }
                return; // 게이지가 오르지 않도록 즉시 종료
            }

            // 보스가 죽었거나 없을 때 정상적으로 게이지가 오르는 로직
            currentCaptureTime += Time.deltaTime;
            float progress = currentCaptureTime / requiredTime;

            UpdateProgressText(progress);

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
                onCaptureComplete?.Invoke();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isCaptured) return;

        if (other.CompareTag("Player"))
        {
            // 보스 경고 텍스트가 떠있었다면, 발판에서 나갔을 때 원래 퍼센트 텍스트로 원상복구 해줍니다.
            if (StageRunManager.Instance != null && StageRunManager.Instance.isBossActive)
            {
                if (progressText != null) progressText.color = originalTextColor;
                UpdateProgressText(currentCaptureTime / requiredTime);
            }

            if (resetOnLeave)
            {
                currentCaptureTime = 0f;

                if (padRenderer != null)
                {
                    padRenderer.GetPropertyBlock(propBlock);
                    propBlock.SetColor(colorPropertyID, Color.white);
                    padRenderer.SetPropertyBlock(propBlock);
                }
                UpdateProgressText(0f); // 리셋 시 텍스트도 0%로
            }
        }
    }

    private void UpdateProgressText(float progress)
    {
        if (progressText != null)
        {
            int percent = Mathf.Clamp(Mathf.FloorToInt(progress * 100f), 0, 100);

            if (isCaptured)
            {
                progressText.text = "100%";
                progressText.color = Color.green;
            }
            else
            {
                progressText.text = $"{percent}%";
                // 정상 진행 중일 때는 기존 색상 유지
                if (progressText.color == Color.red) progressText.color = originalTextColor;
            }
        }
    }
}