using UnityEngine;

public class DroneUnit : MonoBehaviour
{
    [Header("Follow")]
    public float followSmoothTime = 0.15f;

    private Vector3 followVelocity;

    private Transform owner;

    private GameObject projectilePrefab;
    private ParticleSystem shootParticlePrefab;

    private float damage;
    private float attackCooldown;
    private float projectileSpeed;
    private float projectileRange;
    private float detectRadius;

    private Vector3 offset;
    private float attackTimer;

    public void Initialize(
        Transform newOwner,
        GameObject newProjectilePrefab,
        ParticleSystem newShootParticlePrefab,
        float newDamage,
        float newAttackCooldown,
        float newProjectileSpeed,
        float newProjectileRange,
        float newDetectRadius,
        Vector3 newOffset
    )
    {
        owner = newOwner;
        projectilePrefab = newProjectilePrefab;
        shootParticlePrefab = newShootParticlePrefab;

        damage = newDamage;
        attackCooldown = newAttackCooldown;
        projectileSpeed = newProjectileSpeed;
        projectileRange = newProjectileRange;
        detectRadius = newDetectRadius;

        offset = newOffset;
    }

    private void Update()
    {
        if (owner == null)
            return;

        FollowOwner();
        TryAttack();
    }

    private void FollowOwner()
    {
        Vector3 worldOffset =
            owner.TransformDirection(offset);

        Vector3 targetPosition =
            owner.position + worldOffset;

        Vector3 moveDirection =
            targetPosition - transform.position;

        transform.position =
            Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref followVelocity,
                followSmoothTime
            );

        moveDirection.y = 0f;

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(moveDirection.normalized);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    8f * Time.deltaTime
                );
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
        {
            Debug.LogWarning("[DroneUnit] Projectile Prefab 없음");
            return;
        }

        Vector3 direction =
            target.position - transform.position;

        direction.y = 0f;

        if (direction == Vector3.zero)
            return;

        Quaternion rotation =
            Quaternion.LookRotation(direction.normalized);

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

        DroneProjectile projectile =
            projectileObject.GetComponent<DroneProjectile>();

        if (projectile == null)
        {
            Debug.LogWarning("[DroneUnit] DroneProjectile 없음");
            return;
        }

        projectile.Initialize(
            damage,
            projectileSpeed,
            projectileRange,
            direction.normalized,
            target,
            detectRadius
        );
    }

    private Transform FindClosestTarget()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectRadius
        );

        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            float distance =
                (hit.transform.position - transform.position).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = hit.transform;
            }
        }

        return closestTarget;
    }
}