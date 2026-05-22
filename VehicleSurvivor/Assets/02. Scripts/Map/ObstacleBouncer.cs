using UnityEngine;

public class ObstacleBouncer : MonoBehaviour
{
    [Header("Bounce Settings")]
    public float bounceForce = 20f;
    public float bounceHeight = 5f;

    // [핵심 추가] 장애물의 실제 이동 방향을 추적하기 위한 변수
    private Vector3 lastPosition;
    private Vector3 currentMoveDirection;

    private void Start()
    {
        // 시작할 때 현재 위치를 기억해 둡니다.
        lastPosition = transform.position;
    }

    private void Update()
    {
        // 매 프레임 장애물이 어느 방향으로 움직이고 있는지 계산하여 기억해 둡니다.
        Vector3 movement = transform.position - lastPosition;

        // 아주 미세한 움직임이 아닐 때만 방향을 갱신합니다. (가만히 멈출 때 방향이 초기화되는 것 방지)
        if (movement.sqrMagnitude > 0.0001f)
        {
            currentMoveDirection = movement.normalized;
        }

        lastPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerExternalMechanics mechanics = other.GetComponent<PlayerExternalMechanics>();
            if (mechanics != null)
            {
                Vector3 bounceDirection;

                // 1. 망치처럼 '움직이는' 장애물이라면 -> 망치가 휩쓸고 가는 방향 그대로 날려버림!
                if (currentMoveDirection != Vector3.zero)
                {
                    bounceDirection = currentMoveDirection;
                }
                // 2. 가시밭처럼 '가만히 있는' 고정 장애물이라면 -> 기존처럼 중심에서 밀어냄
                else
                {
                    bounceDirection = (other.transform.position - transform.position).normalized;
                }

                bounceDirection.y = 0f; // 수평으로만 날아가게 Y축 제거 (상승은 ApplyTargetHeightJump가 담당)

                // 혹시라도 방향 벡터가 0이 되는 에러 튕김 방지
                if (bounceDirection == Vector3.zero) bounceDirection = transform.forward;

                // 역학 스크립트의 함수를 사용해 계산된 방향으로 날려버립니다.
                mechanics.ApplyTargetHeightJump(bounceHeight, bounceForce, bounceDirection.normalized);
            }
        }
    }
}