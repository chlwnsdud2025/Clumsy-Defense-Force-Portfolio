using UnityEngine;

public class ProjectileWeapon : WeaponBase
{
    [Header("Projectile References")]
    public GameObject projectilePrefab;
    public ParticleSystem shootParticlePrefab;

    [Header("Fire Point")]
    public Transform firePoint;

    protected override void Attack(WeaponLevelData data)
    {
        FireProjectiles(
            GetFinalDamage(data.Damage),
            GetProjectileSpeed(data),
            Mathf.Max(1, GetFinalProjectileCount(data.ProjectileCount)),
            data.SpreadAngle
        );
    }

    private void FireProjectiles(
        float finalDamage,
        float projectileSpeed,
        int count,
        float spreadAngleValue
    )
    {
        Transform resolvedFirePoint = ResolveFirePoint(firePoint);

        if (projectilePrefab == null)
        {
            Debug.LogWarning("Projectile prefab is missing.");
            return;
        }

        if (resolvedFirePoint == null)
        {
            Debug.LogWarning("Fire point is missing.");
            return;
        }

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

            Quaternion bulletRotation =
                resolvedFirePoint.rotation *
                Quaternion.Euler(0f, angle, 0f);

            GameObject bulletObject = Instantiate(
                projectilePrefab,
                resolvedFirePoint.position,
                bulletRotation
            );

            ProjectileBullet bullet = bulletObject.GetComponent<ProjectileBullet>();

            if (bullet != null)
            {
                bullet.Init(
                    finalDamage,
                    projectileSpeed,
                    bulletRotation * Vector3.forward
                );
            }
        }
    }

    private float GetProjectileSpeed(WeaponLevelData data)
    {
        return data.ProjectileSpeed > 0f ? data.ProjectileSpeed : data.BulletSpeed;
    }
}
