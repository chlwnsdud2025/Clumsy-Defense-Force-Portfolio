using UnityEngine;

public class DroneUnit : MonoBehaviour
{
    [Header("Follow Physics")]
    public float followSpeed = 20f;
    [Tooltip("궤적을 따라가는 반응 속도 (낮을수록 칼같이 따라감)")]
    public float smoothTime = 0.1f;

    [Header("Rotation Setup")]
    public float lookRotationSpeed = 12f;

    private Transform owner;
    private GameObject projectilePrefab;
    private ParticleSystem shootParticlePrefab;
    private LayerMask targetLayer;

    private DamageInfo damageInfo;
    private float attackCooldown;
    private float projectileSpeed;
    private float projectileRange;
    private float detectRadius;

    private float attackTimer;

    // 기차(Snake) 이동 관련
    private Vector3 targetFollowPosition;
    private Vector3 currentVelocity;

    public void Initialize(
        Transform newOwner,
        GameObject newProjectilePrefab,
        ParticleSystem newShootParticlePrefab,
        LayerMask newTargetLayer,
        DamageInfo newDamageInfo,
        float newAttackCooldown,
        float newProjectileSpeed,
        float newProjectileRange,
        float newDetectRadius
    )
    {
        owner = newOwner;
        projectilePrefab = newProjectilePrefab;
        shootParticlePrefab = newShootParticlePrefab;
        targetLayer = newTargetLayer;
        damageInfo = newDamageInfo;
        attackCooldown = newAttackCooldown;
        projectileSpeed = newProjectileSpeed;
        projectileRange = newProjectileRange;
        detectRadius = newDetectRadius;

        targetFollowPosition = transform.position; // 처음 생성 시엔 제자리 대기
    }

    // DroneWeapon에서 매 프레임 이 드론이 가야 할 목표 궤적 지점을 갱신해줌
    public void SetFollowTarget(Vector3 pos)
    {
        targetFollowPosition = pos;
    }

    private void Update()
    {
        if (owner == null) return;

        FollowPath();
        RotateTowardsAttackDirection();
        TryAttack();
    }

    private void FollowPath()
    {
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetFollowPosition,
            ref currentVelocity,
            smoothTime,
            followSpeed
        );
    }

    private void RotateTowardsAttackDirection()
    {
        Transform target = FindClosestTarget();

        if (target != null)
        {
            // 적이 있으면 적을 조준
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    lookRotationSpeed * Time.deltaTime
                );
            }
        }
        else
        {
            // 적이 없으면 자신이 이동하는 경로(기차 진행 방향)를 자연스럽게 바라봄
            if (currentVelocity.sqrMagnitude > 0.1f)
            {
                Quaternion moveRotation = Quaternion.LookRotation(currentVelocity.normalized);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    moveRotation,
                    lookRotationSpeed * Time.deltaTime
                );
            }
            else
            {
                // 완전히 정지해있을 땐 주인의 정면과 일치시킴
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    owner.rotation,
                    lookRotationSpeed * Time.deltaTime
                );
            }
        }
    }

    private void TryAttack()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer < attackCooldown)
            return;

        Transform target = FindClosestTarget();
        if (target == null)
            return;

        attackTimer = 0f;
        FireProjectile(target);
    }

    private void FireProjectile(Transform target)
    {
        if (projectilePrefab == null)
            return;

        Vector3 direction = target.position - transform.position;
        Quaternion rotation = Quaternion.LookRotation(direction.normalized);

        if (shootParticlePrefab != null)
        {
            Instantiate(
                shootParticlePrefab,
                transform.position,
                rotation
            );
        }

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            transform.position,
            rotation
        );

        DroneProjectile projectile = projectileObject.GetComponent<DroneProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(
                targetLayer,
                damageInfo,
                projectileSpeed,
                projectileRange,
                direction.normalized,
                target,
                detectRadius
            );
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
            if (IsPlayerLayer(hit))
                continue;

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

    private bool IsPlayerLayer(Collider other)
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer == -1) return false;
        if (other.gameObject.layer == playerLayer) return true;

        Transform root = other.transform.root;
        return root.gameObject.layer == playerLayer;
    }
}