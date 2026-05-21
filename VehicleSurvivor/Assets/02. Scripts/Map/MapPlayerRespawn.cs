using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class MapPlayerRespawn : MonoBehaviour
{
    [SerializeField] private Transform currentRespawnPoint;

    private CharacterController controller;

    public Transform CurrentRespawnPoint => currentRespawnPoint;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public void SetRespawnPoint(Transform respawnPoint)
    {
        if (respawnPoint != null)
            currentRespawnPoint = respawnPoint;
    }

    public void Respawn()
    {
        if (currentRespawnPoint == null)
        {
            Debug.LogWarning("[MapPlayerRespawn] Respawn point is not assigned.", this);
            return;
        }

        bool wasEnabled = controller != null && controller.enabled;
        if (wasEnabled)
            controller.enabled = false;

        transform.SetPositionAndRotation(currentRespawnPoint.position, currentRespawnPoint.rotation);

        if (wasEnabled)
            controller.enabled = true;
    }
}
