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
            PlayerEnvironmentReceiver receiver = other.GetComponentInParent<PlayerEnvironmentReceiver>();
            if (receiver != null)
            {
                Vector3 launchDirection = transform.up;

                // [핵심 수정] 마지막 두 파라미터를 (true, true)로 설정합니다!
                // 첫 번째 true: Y축 낙하 속도 0으로 초기화 (무조건 동일한 높이로 점프)
                // 두 번째 true: XZ축 달리기 속도 0으로 초기화 (달려오던 관성을 무시하고 점프대 방향으로만 날아감)
                receiver.ApplyTargetHeightJump(targetHeight, horizontalForce, launchDirection.normalized, true, true);

                Debug.Log($"점프대 작동! 궤적 완전 고정. 방향: {launchDirection}, 밀어내는 힘: {horizontalForce}");
            }
        }
    }
}