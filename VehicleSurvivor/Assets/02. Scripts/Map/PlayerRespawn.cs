using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn Info")]
    [Tooltip("현재 저장된 스폰 위치 (시작할 때는 광장 중앙 등으로 지정)")]
    public Transform currentRespawnPoint;

    private CharacterController _controller;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
    }

    // OutZonePlane에 닿았을 때 호출될 함수
    public void Respawn()
    {
        if (currentRespawnPoint != null)
        {
            // CharacterController의 텔레포트 무시 현상을 막기 위해 잠시 끕니다.
            _controller.enabled = false;

            // 위치와 바라보는 방향을 목표 스폰 포인트로 텔레포트!
            transform.position = currentRespawnPoint.position;
            transform.rotation = currentRespawnPoint.rotation;

            // 다시 켜줍니다.
            _controller.enabled = true;

            Debug.Log("저장된 스폰 포인트로 안전하게 이동했습니다.");
        }
        else
        {
            Debug.LogWarning("저장된 스폰 포인트가 없습니다!");
        }
    }
}