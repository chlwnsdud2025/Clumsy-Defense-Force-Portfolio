using UnityEngine;

[DisallowMultipleComponent]
public sealed class MapStageCheckpoint : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        MapPlayerRespawn respawn = other.GetComponentInParent<MapPlayerRespawn>();
        if (respawn != null)
            respawn.SetRespawnPoint(respawnPoint);
    }
}
