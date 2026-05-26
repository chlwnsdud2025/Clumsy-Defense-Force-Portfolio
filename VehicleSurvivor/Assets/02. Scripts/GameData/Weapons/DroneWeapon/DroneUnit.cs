using UnityEngine;

public class DroneUnit : MonoBehaviour
{
    [Header("Follow")]
    public float followSmoothTime =
        0.15f;

    private Vector3 followVelocity;

    private Transform owner;

    private GameObject projectilePrefab;
    private ParticleSystem shootParticlePrefab;

    private LayerMask targetLayer;

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
        LayerMask newTargetLayer,
        float newDamage,
        float newAttackCooldown,
        float newProjectileSpeed,
        float newProjectileRange,
        float newDetectRadius,
        Vector3 newOffset
    )
    {
        owner = newOwner;

        projectilePrefab =
            newProjectilePrefab;

        shootParticlePrefab =
            newShootParticlePrefab;

        targetLayer =
            newTargetLayer;

        damage =
            newDamage;

        attackCooldown =
            newAttackCooldown;

        projectileSpeed =
            newProjectileSpeed;

        projectileRange =
            newProjectileRange;

        detectRadius =
            newDetectRadius;

        offset =
            newOffset;
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
            owner.TransformDirection(
                offset
            );

        Vector3 targetPosition =
            owner.position +
            worldOffset;

        Vector3 moveDirection =
            targetPosition -
            transform.position;

        transform.position =
            Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref followVelocity,
                followSmoothTime
            );

        moveDirection.y = 0f;

        if (
            moveDirection !=
            Vector3.zero
        )
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    moveDirection.normalized
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    8f *
                    Time.deltaTime
                );
        }
    }

    private void TryAttack()
    {
        attackTimer +=
            Time.deltaTime;

        if (
            attackTimer <
            attackCooldown
        )
        {
            return;
        }

        Transform target =
            FindClosestTarget();

        if (target == null)
            return;

        attackTimer = 0f;

        FireProjectile(
            target
        );
    }

    private void FireProjectile(
        Transform target
    )
    {
        if (
            projectilePrefab ==
            null
        )
            return;

        Vector3 direction =
            target.position -
            transform.position;

        Quaternion rotation =
            Quaternion.LookRotation(
                direction.normalized
            );

        GameObject projectileObject =
            Instantiate(
                projectilePrefab,
                transform.position,
                rotation
            );

        DroneProjectile projectile =
            projectileObject
            .GetComponent<DroneProjectile>();

        projectile.Initialize(
            targetLayer,
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
        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                detectRadius,
                targetLayer
            );

        Transform closest =
            null;

        float closestDistance =
            Mathf.Infinity;

        foreach (
            Collider hit
            in hits
        )
        {
            float distance =
                (
                    hit.transform.position -
                    transform.position
                ).sqrMagnitude;

            if (
                distance <
                closestDistance
            )
            {
                closestDistance =
                    distance;

                closest =
                    hit.transform;
            }
        }

        return closest;
    }
}