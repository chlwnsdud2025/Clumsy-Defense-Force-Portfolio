using UnityEngine;
using UnityEngine.Serialization;

public class HomingWeapon : WeaponBase
{
    [Header("Homing References")]
    public GameObject homingProjectilePrefab;
    public ParticleSystem shootParticlePrefab;
    public GameObject hitEffectPrefab;

    [Header("Fire Point")]
    public Transform firePoint;

    [Header("Target Settings")]
    public LayerMask targetLayer;

    [Header("Target Settings")]
    [FormerlySerializedAs("detectRadius")]
    public float fallbackDetectRadius = 15f;

    protected override void Attack(WeaponLevelData data)
    {
        FireHomingProjectiles(
            GetFinalDamage(data.Damage),
            GetProjectileSpeed(data),
            Mathf.Max(1, GetFinalProjectileCount(data.ProjectileCount)),
            data.SpreadAngle,
            GetDetectRadius(data)
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
        Transform resolvedFirePoint = ResolveFirePoint(firePoint);

        if (homingProjectilePrefab == null)
            return;

        if (resolvedFirePoint == null)
            return;

        Transform target = FindClosestTarget(detectRange);

        if (target == null)
            return;

        if (shootParticlePrefab != null)
        {
            Instantiate(
                shootParticlePrefab,
                resolvedFirePoint.position,
                resolvedFirePoint.rotation
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
                resolvedFirePoint.rotation * Quaternion.Euler(0f, angle, 0f);

            GameObject projectile = Instantiate(
                homingProjectilePrefab,
                resolvedFirePoint.position,
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
                    hitEffectPrefab
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
        return data.ProjectileSpeed > 0f ? data.ProjectileSpeed : data.BulletSpeed;
    }

    private float GetDetectRadius(WeaponLevelData data)
    {
        return data.Range > 0f ? data.Range : fallbackDetectRadius;
    }
}
