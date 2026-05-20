using UnityEngine;

[DisallowMultipleComponent]
public sealed class MapOutZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        MapPlayerRespawn respawn = other.GetComponentInParent<MapPlayerRespawn>();
        if (respawn != null)
            respawn.Respawn();
    }
}
