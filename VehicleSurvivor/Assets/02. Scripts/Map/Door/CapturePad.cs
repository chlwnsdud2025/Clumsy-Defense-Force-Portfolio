using UnityEngine;
using UnityEngine.Events;

public class CapturePad : MonoBehaviour
{
    [Header("Capture Settings")]
    public float requiredTime = 3.0f;
    public bool resetOnLeave = false;

    [Header("Events")]
    public UnityEvent onCaptureComplete;

    private float currentCaptureTime = 0f;
    private bool isCaptured = false;
    private Renderer padRenderer;

    // [최적화 추가]
    private MaterialPropertyBlock propBlock;
    private int colorPropertyID;

    void Start()
    {
        padRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
        colorPropertyID = Shader.PropertyToID("_Color"); // URP라면 "_BaseColor"일 수 있습니다.
    }

    private void OnTriggerStay(Collider other)
    {
        if (isCaptured) return;

        if (other.CompareTag("Player"))
        {
            currentCaptureTime += Time.deltaTime;
            float progress = currentCaptureTime / requiredTime;

            // [최적화 수정] PropertyBlock을 사용한 색상 변경
            Color currentColor = Color.Lerp(Color.gray, Color.green, progress);
            padRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(colorPropertyID, currentColor);
            padRenderer.SetPropertyBlock(propBlock);

            if (currentCaptureTime >= requiredTime)
            {
                isCaptured = true;

                padRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor(colorPropertyID, Color.green);
                padRenderer.SetPropertyBlock(propBlock);

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

                // [최적화 수정]
                padRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor(colorPropertyID, Color.gray);
                padRenderer.SetPropertyBlock(propBlock);
            }
        }
    }
}