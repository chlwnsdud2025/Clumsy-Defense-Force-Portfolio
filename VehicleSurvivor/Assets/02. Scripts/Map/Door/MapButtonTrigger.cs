using UnityEngine;

[DisallowMultipleComponent]
public sealed class MapButtonTrigger : MonoBehaviour
{
    [SerializeField] private MapDoorController targetDoor;
    [SerializeField] private float pressDepth = 0.5f;
    [SerializeField] private Renderer targetRenderer;

    private bool isPressed;
    private Vector3 originalPosition;

    private void Awake()
    {
        originalPosition = transform.position;

        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isPressed || other.GetComponentInParent<Player>() == null)
            return;

        isPressed = true;
        transform.position = originalPosition - Vector3.up * pressDepth;

        if (targetRenderer != null)
            targetRenderer.material.color = Color.green;

        if (targetDoor != null)
            targetDoor.OpenDoor();
    }
}
