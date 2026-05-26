using UnityEngine;

public class DroneProjectile : MonoBehaviour
{
    [Header("Homing")]
    public float rotateSpeed = 8f;

    private LayerMask targetLayer;
    private DamageInfo damageInfo;
    private float speed;
    private float maxDistance;

    private Vector3 moveDirection;

    // [수정] 스폰 위치 대신 '누적 이동 거리'를 추적합니다.
    private float traveledDistance;

    private Transform target;
    private float detectRadius;

    public void Initialize(
        LayerMask newTargetLayer,
        DamageInfo newDamageInfo,
        float newSpeed,
        float newMaxDistance,
        Vector3 newMoveDirection,
        Transform newTarget,
        float newDetectRadius
    )
    {
        targetLayer = newTargetLayer;
        damageInfo = newDamageInfo;
        speed = newSpeed;
        maxDistance = newMaxDistance;
        moveDirection = newMoveDirection.normalized;

        traveledDistance = 0f; // 초기화

        target = newTarget;
        detectRadius = newDetectRadius;
    }

    private void Update()
    {
        // [수정] 타겟이 비활성화(풀링/사망처리) 되었으면 타겟을 초기화합니다.
        if (target != null && !target.gameObject.activeInHierarchy)
        {
            target = null;
        }

        if (target == null)
        {
            target = FindClosestTarget();
        }

        if (target != null)
        {
            Vector3 targetPos = target.position;
            targetPos.y += 0.8f;

            Vector3 targetDirection = targetPos - transform.position;

            // [수정] 타겟 중심 반경 0.5 이내로 진입했는데 충돌 처리가 안 났다면 (적이 이미 죽었거나 콜라이더가 없음) 강제 파괴합니다.
            if (targetDirection.sqrMagnitude < 0.25f)
            {
                Destroy(gameObject);
                return;
            }

            if (targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection.normalized);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotateSpeed * Time.deltaTime
                );

                moveDirection = transform.forward;
            }
        }

        // 이동 처리
        float moveStep = speed * Time.deltaTime;
        transform.position += moveDirection * moveStep;

        // [수정] 직선거리가 아닌 '실제로 비행한 누적 거리'를 체크하여 확실하게 파괴되도록 합니다.
        traveledDistance += moveStep;
        if (traveledDistance >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private Transform FindClosestTarget()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectRadius,
            targetLayer
        );

        Transform closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            // [수정] 재탐색 시 플레이어 본인은 타겟팅하지 않음
            if (IsPlayerLayer(hit))
                continue;

            // [수정] IDamageable이 있는지(유효한 적인지) 확인
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null)
                continue;

            float distance = (hit.transform.position - transform.position).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = hit.transform;
            }
        }

        return closest;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerLayer(other))
        {
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        damageable.TakeDamage(damageInfo);
        Debug.Log($"드론 데미지 들어감: {damageInfo.Amount}");

        Destroy(gameObject);
    }

    // [추가] 통합된 플레이어 판별 로직
    private bool IsPlayerLayer(Collider other)
    {
        int playerLayer = LayerMask.NameToLayer("Player");

        if (playerLayer == -1)
            return false;

        if (other.gameObject.layer == playerLayer)
            return true;

        Transform root = other.transform.root;
        return root.gameObject.layer == playerLayer;
    }
}