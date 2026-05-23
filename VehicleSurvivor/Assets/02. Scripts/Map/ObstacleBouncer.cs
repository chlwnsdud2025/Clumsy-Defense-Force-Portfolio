using UnityEngine;

public class ObstacleBouncer : MonoBehaviour
{
    public float bounceForce = 20f;
    public float bounceHeight = 5f;

    // Is Trigger를 쓰므로 Collision 대신 Trigger를 사용합니다.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerEnvironmentReceiver receiver = other.GetComponentInParent<PlayerEnvironmentReceiver>();
            if (receiver != null)
            {
                // [핵심] 이번에는 방해물의 중심(본체)이 아니라, 
                // 부딪힌 트리거 오브젝트의 위치를 기준으로 방향을 잡아야 더 정확합니다.
                Vector3 bounceDirection = (other.transform.position - transform.position).normalized;
                bounceDirection.y = 0f;

                receiver.ApplyTargetHeightJump(bounceHeight, bounceForce, bounceDirection);
            }
        }
    }
}