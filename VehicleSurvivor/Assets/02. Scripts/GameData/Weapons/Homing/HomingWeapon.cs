using UnityEngine;

public class HomingWeapon : WeaponBase
{
    [Header("Homing Data")]
    public HomingWeaponDataSO homingData;

    [Header("Fire Point")]
    public Transform firePoint;

    [Header("Target Settings")]
    public LayerMask targetLayer;

    [Header("Legacy Stats")]
    public float damage = 10f;
    public float cooldown = 1f;
    public float bulletSpeed = 15f;
    public float detectRadius = 15f;
    public int projectileCount = 1;
    public float spreadAngle = 15f;


    protected override void Update()
    {
        if (HasConfiguredLevelData())
        {
            base.Update();
            return;
        }

        attackTimer += Time.deltaTime;

        if (attackTimer >= GetFinalCooldown(cooldown))
        {
            AttackLegacy();
            attackTimer = 0f;
        }
    }

    protected override void Attack(WeaponLevelData data)
    {
        FireHomingProjectiles(
            GetFinalDamage(data.Damage),
            GetProjectileSpeed(data),
            Mathf.Max(1, GetFinalProjectileCount(data.ProjectileCount)),
            data.spreadAngle,
            GetDetectRadius(data)
        );
    }

    private void AttackLegacy()
    {
        FireHomingProjectiles(
            GetFinalDamage(damage),
            bulletSpeed,
            Mathf.Max(1, GetFinalProjectileCount(projectileCount)),
            spreadAngle,
            detectRadius
        );
    }

    private void FireHomingProjectiles(
        float finalDamage,
        float projectileSpeed,
        int count,
        float spreadAngleValue,
        float detectRange
    )
    {
        if (homingData == null || homingData.homingProjectilePrefab == null)
            return;

        if (firePoint == null)
            return;

        Transform target = FindClosestTarget(detectRange);

        if (target == null)
            return;

        if (homingData.shootParticlePrefab != null)
        {
            Instantiate(
                homingData.shootParticlePrefab,
                firePoint.position,
                firePoint.rotation
            );
        }

        for (int i = 0; i < count; i++)
        {
            float angle = 0f;

            if (count > 1)
            {
                float centerIndex = (count - 1) * 0.5f;
                angle = (i - centerIndex) * spreadAngleValue;
            }

            Quaternion rotation =
                firePoint.rotation * Quaternion.Euler(0f, angle, 0f);

            GameObject projectile = Instantiate(
                homingData.homingProjectilePrefab,
                firePoint.position,
                rotation
            );

            HomingProjectile homing = projectile.GetComponent<HomingProjectile>();

            if (homing != null)
            {
                homing.Init(
                    target,
                    finalDamage,
                    projectileSpeed,
                    targetLayer,
                    homingData.hitEffectPrefab
                );
            }
        }
    }

    private Transform FindClosestTarget(float detectRange)
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectRange,
            targetLayer
        );

        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            Transform targetTransform = hit.transform;
            float distance = (targetTransform.position - transform.position).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = targetTransform;
            }
        }

        return closestTarget;
    }

    private float GetProjectileSpeed(WeaponLevelData data)
    {
        if (data == null)
            return bulletSpeed;

        return data.ProjectileSpeed > 0f ? data.ProjectileSpeed : data.bulletSpeed;
    }

    private float GetDetectRadius(WeaponLevelData data)
    {
        if (data == null)
            return detectRadius;

        return data.Range > 0f ? data.Range : detectRadius;
    }
}
