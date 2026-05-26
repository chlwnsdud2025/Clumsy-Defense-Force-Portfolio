using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public sealed class MapPlayerRespawn : MonoBehaviour
{
    [SerializeField] private Transform currentRespawnPoint;

    private PlayerController _controller;

    public Transform CurrentRespawnPoint => currentRespawnPoint;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
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

        _controller.Teleport(currentRespawnPoint.position, currentRespawnPoint.rotation);
    }
}
