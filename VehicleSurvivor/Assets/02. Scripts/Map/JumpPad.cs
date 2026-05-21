using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [Header("점프대 설정")]
    [Tooltip("목표로 하는 정확한 Y축 점프 높이 (무조건 위로 솟는 높이)")]
    public float targetHeight = 15f;

    [Tooltip("점프대의 '파란색 화살표(앞)' 방향으로 밀어내는 수평 힘")]
    public float horizontalForce = 15f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerExternalMechanics mechanics = other.GetComponent<PlayerExternalMechanics>();
            if (mechanics != null)
            {
                // [핵심 수정] 점프대가 바라보는 절대적인 '앞(Forward)' 방향을 넘겨줍니다!
                // 이렇게 하면 플레이어가 어느 쪽을 보며 밟든 무조건 이 방향으로 날아갑니다.
                Vector3 launchDirection = transform.up;

                mechanics.ApplyTargetHeightJump(targetHeight, horizontalForce, launchDirection.normalized);

                Debug.Log($"점프대 작동! 방향: {launchDirection}, 밀어내는 힘: {horizontalForce}");
            }
        }
    }
}