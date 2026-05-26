using UnityEngine;

public class BouncyProjectileWeapon : WeaponBase
{
    [Header("Bouncy Projectile References")]
    public GameObject bouncyProjectilePrefab;
    public ParticleSystem shootParticlePrefab;

    [Header("Fire Point")]
    public Transform firePoint;

    [Header("Bounce Layer")]
    public LayerMask bounceLayer;

    [Header("Rotation")]
    public Vector3 projectileRotationOffset;

    [Header("Bouncy Feeling")]
    public float upwardForce = 5f;
    public float gravity = 18f;
    public float bounciness = 0.85f;

    protected override void Attack(WeaponLevelData data)
    {
        if (data == null)
            return;

        FireProjectiles(
            GetFinalDamage(data.Damage),
            GetProjectileSpeed(data),
            Mathf.Max(1, GetFinalProjectileCount(data.ProjectileCount)),
            data.SpreadAngle,
            data.Range,
            data.BounceCount,
            GetFinalWeaponScale(data.Size)
        );
    }

    private void FireProjectiles(
        float finalDamage,
        float projectileSpeed,
        int count,
        float spreadAngleValue,
        float maxTravelDistance,
        int bounceCount,
        float projectileSize
    )
    {
        Transform resolvedFirePoint =
            ResolveFirePoint(firePoint);

        if (bouncyProjectilePrefab == null)
        {
            Debug.LogWarning("[BouncyProjectileWeapon] Bouncy Projectile Prefab 없음");
            return;
        }

        if (resolvedFirePoint == null)
        {
            Debug.LogWarning("[BouncyProjectileWeapon] Fire Point 없음");
            return;
        }

        Quaternion baseRotation =
            resolvedFirePoint.rotation;

        if (shootParticlePrefab != null)
        {
            Instantiate(
                shootParticlePrefab,
                resolvedFirePoint.position,
                baseRotation
            );
        }

        for (int i = 0; i < count; i++)
        {
            float angle = 0f;

            if (count > 1)
            {
                float centerIndex =
                    (count - 1) * 0.5f;

                angle =
                    (i - centerIndex) * spreadAngleValue;
            }

            Quaternion shootRotation =
                baseRotation *
                Quaternion.Euler(0f, angle, 0f);

            Quaternion visualRotation =
                shootRotation *
                Quaternion.Euler(projectileRotationOffset);

            GameObject projectileObject =
                Instantiate(
                    bouncyProjectilePrefab,
                    resolvedFirePoint.position,
                    visualRotation
                );

            projectileObject.transform.localScale =
                Vector3.one * projectileSize;

            BouncyProjectile projectile =
                projectileObject.GetComponent<BouncyProjectile>();

            if (projectile == null)
            {
                Debug.LogWarning("[BouncyProjectileWeapon] BouncyProjectile 컴포넌트 없음");
                continue;
            }

            Vector3 startVelocity =
                shootRotation * Vector3.forward * projectileSpeed;

            startVelocity.y += upwardForce;

            projectile.Initialize(
                finalDamage,
                startVelocity,
                maxTravelDistance,
                bounceCount,
                bounceLayer,
                gravity,
                bounciness
            );
        }
    }

    private float GetProjectileSpeed(WeaponLevelData data)
    {
        return data.ProjectileSpeed > 0f
            ? data.ProjectileSpeed
            : data.BulletSpeed;
    }
}