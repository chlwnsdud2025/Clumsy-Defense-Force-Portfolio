using UnityEngine;

public class ObstacleBouncer : MonoBehaviour
{
    public float bounceForce = 20f;
    public float bounceHeight = 5f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerEnvironmentReceiver receiver = other.GetComponentInParent<PlayerEnvironmentReceiver>();
            if (receiver != null)
            {
                Collider myCollider = GetComponent<Collider>();
                Vector3 closestPoint = transform.position;

                // [핵심] 장애물의 중심이 아닌 플레이어와 충돌한 표면 좌표를 구합니다.
                if (myCollider != null)
                {
                    closestPoint = myCollider.ClosestPoint(other.transform.position);
                }

                Vector3 bounceDirection = other.transform.position - closestPoint;
                bounceDirection.y = 0f;

                // 드물게 중심점과 완전히 일치할 때의 방어 코드
                if (bounceDirection == Vector3.zero)
                {
                    bounceDirection = other.transform.position - transform.position;
                    bounceDirection.y = 0f;
                }

                // [핵심 수정] 끝의 두 파라미터 (true, true)
                // Y축 낙하 속도 무시, XZ 수평 달리기 속도 무시 => 오직 bounce 방향으로만 튕김!
                receiver.ApplyTargetHeightJump(bounceHeight, bounceForce, bounceDirection.normalized, true, true);
            }
        }
    }
}