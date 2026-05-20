using UnityEngine;

public class OutZonePlane : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerRespawn respawn = other.GetComponent<PlayerRespawn>();
            if (respawn != null)
            {
                // 부활(순간이동) 함수 실행!
                respawn.Respawn();
            }
        }
    }
}