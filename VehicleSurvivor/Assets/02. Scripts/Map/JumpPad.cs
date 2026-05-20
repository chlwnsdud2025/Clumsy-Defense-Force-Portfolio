using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [Header("점프대 설정")]
    [Tooltip("목표로 하는 정확한 Y축 점프 높이 (무조건 위로 솟는 높이)")]
    public float targetHeight = 15f;

    [Tooltip("점프대가 바라보는 방향으로 밀어내는 수평 힘")]
    public float horizontalForce = 0f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerExternalMechanics mechanics = other.GetComponent<PlayerExternalMechanics>();
            if (mechanics != null)
            {
                // [핵심 변경점] Vector3.up 대신 transform.up(점프대의 로컬 Y축)을 사용합니다!
                // 만약 점프대 모델링이 누워있어서 Z축(앞쪽)으로 쏴야 한다면 transform.forward 를 쓰시면 됩니다.
                Vector3 localDirection = transform.up;

                mechanics.ApplyTargetHeightJump(targetHeight, horizontalForce, localDirection.normalized);

                Debug.Log($"점프대 작동! 높이: {targetHeight}, 밀어내는 힘: {horizontalForce}");
            }
        }
    }
}