using UnityEngine;

public class TestOrbitDroneUnit : MonoBehaviour
{
    [Header("Spin")]
    public float selfSpinSpeed = 360f;

    private Transform owner;

    private GameObject projectilePrefab;
    private ParticleSystem shootParticlePrefab;
    private LayerMask targetLayer;

    private DamageInfo damageInfo;
    private float attackCooldown;
    private float projectileSpeed;
    private float projectileRange;
    private float detectRadius;

    private float orbitRadius;
    private float orbitRotateSpeed;
    private float orbitHeight;
    private float angle;

    private float attackTimer;

    public void Initialize(
        Transform newOwner,
        GameObject newProjectilePrefab,
        ParticleSystem newShootParticlePrefab,
        LayerMask newTargetLayer,
        DamageInfo newDamageInfo,
        float newAttackCooldown,
        float newProjectileSpeed,
        float newProjectileRange,
        float newDetectRadius,
        float newOrbitRadius,
        float newOrbitRotateSpeed,
        float newOrbitHeight,
        float newStartAngle
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

        orbitRadius = newOrbitRadius;
        orbitRotateSpeed = newOrbitRotateSpeed;
        orbitHeight = newOrbitHeight;
        angle = newStartAngle;

        OrbitAroundOwner();
    }

    private void Update()
    {
        if (owner == null)
            return;

        OrbitAroundOwner();
        SpinSelf();
        TryAttack();
    }

    private void OrbitAroundOwner()
    {
        angle += orbitRotateSpeed * Time.deltaTime;

        float radian = angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(radian),
            0f,
            Mathf.Sin(radian)
        ) * orbitRadius;

        Vector3 targetPosition =
            owner.position + offset;

        targetPosition.y =
            owner.position.y + orbitHeight;

        transform.position = targetPosition;
    }

    private void SpinSelf()
    {
        transform.Rotate(
            Vector3.up,
            selfSpinSpeed * Time.deltaTime,
            Space.Self
        );
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

        Vector3 targetPosition = target.position;
        targetPosition.y += 0.8f;

        Vector3 direction =
            targetPosition - transform.position;

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
            return;

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

    private Transform FindClosestTarget()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectRadius,
            targetLayer
        );

        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
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