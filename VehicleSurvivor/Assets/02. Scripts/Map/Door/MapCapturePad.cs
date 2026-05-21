using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class MapCapturePad : MonoBehaviour
{
    [SerializeField] private float requiredTime = 3f;
    [SerializeField] private bool resetOnLeave;
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private UnityEvent onCaptureComplete;

    private float currentCaptureTime;
    private bool isCaptured;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (isCaptured || other.GetComponentInParent<Player>() == null)
            return;

        currentCaptureTime += Time.deltaTime;
        float progress = requiredTime > 0f ? Mathf.Clamp01(currentCaptureTime / requiredTime) : 1f;

        if (targetRenderer != null)
            targetRenderer.material.color = Color.Lerp(Color.gray, Color.green, progress);

        if (currentCaptureTime < requiredTime)
            return;

        isCaptured = true;
        if (targetRenderer != null)
            targetRenderer.material.color = Color.green;

        onCaptureComplete?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (isCaptured || !resetOnLeave || other.GetComponentInParent<Player>() == null)
            return;

        currentCaptureTime = 0f;
        if (targetRenderer != null)
            targetRenderer.material.color = Color.gray;
    }
}
