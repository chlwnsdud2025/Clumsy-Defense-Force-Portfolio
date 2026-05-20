using UnityEngine;

public class RollingObstacle : MonoBehaviour
{
    [Header("충돌 넉백 설정")]
    public float knockbackForce = 20f;
    public float knockbackHeight = 3f;

    private float _lastHitTime = 0f;

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어를 쳤을 때 넉백!
        if (other.CompareTag("Player") && Time.time >= _lastHitTime + 0.5f)
        {
            PlayerExternalMechanics mechanics = other.GetComponent<PlayerExternalMechanics>();
            if (mechanics != null)
            {
                _lastHitTime = Time.time;

                // 과일이 굴러가는 방향 그대로 플레이어를 튕겨냅니다
                Vector3 pushDirection = other.transform.position - transform.position;
                pushDirection.y = 0f;

                mechanics.ApplyTargetHeightJump(knockbackHeight, knockbackForce, pushDirection.normalized);
            }
        }
    }
}