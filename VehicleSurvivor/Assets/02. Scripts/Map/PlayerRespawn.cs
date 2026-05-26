using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn Info")]
    [Tooltip("현재 저장된 스폰 위치 (시작할 때는 광장 중앙 등으로 지정)")]
    public Transform currentRespawnPoint;

    private PlayerController _controller;

    void Start()
    {
        _controller = GetComponent<PlayerController>();
    }

    public void Respawn()
    {
        if (currentRespawnPoint == null)
        {
            Debug.LogWarning("저장된 스폰 포인트가 없습니다!");
            return;
        }

        _controller.Teleport(currentRespawnPoint.position, currentRespawnPoint.rotation);
        Debug.Log("저장된 스폰 포인트로 안전하게 이동했습니다.");
    }
}
