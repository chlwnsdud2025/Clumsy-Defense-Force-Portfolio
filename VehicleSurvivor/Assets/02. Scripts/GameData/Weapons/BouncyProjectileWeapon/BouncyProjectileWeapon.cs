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

        // 🌟 [핵심 수정 1] SO에서 Size 값을 비워둬서(0) 오브젝트가 투명해지는 현상 방지
        float baseSize = data.Size > 0f ? data.Size : 1f;

        // 🌟 [핵심 수정 2] 바운스 횟수가 0이라서 땅에 닿자마자 파괴되는 현상 방지 (기본 3번 튕김)
        int bounceCount = data.BounceCount > 0 ? data.BounceCount : 3;

        FireProjectiles(
            GetFinalDamage(data.Damage),
            GetProjectileSpeed(data),
            Mathf.Max(1, GetFinalProjectileCount(data.ProjectileCount)),
            data.SpreadAngle,
            data.Range,
            bounceCount,
            GetFinalWeaponScale(baseSize)
        );
    }

    private void FireProjectiles(
        DamageInfo damageInfo,
        float projectileSpeed,
        int count,
        float spreadAngleValue,
        float maxTravelDistance,
        int bounceCount,
        float projectileSize
    )
    {
        Transform resolvedFirePoint = ResolveFirePoint(firePoint);

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

        Quaternion baseRotation = resolvedFirePoint.rotation;

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
                float centerIndex = (count - 1) * 0.5f;
                angle = (i - centerIndex) * spreadAngleValue;
            }

            Quaternion shootRotation = baseRotation * Quaternion.Euler(0f, angle, 0f);
            Quaternion visualRotation = shootRotation * Quaternion.Euler(projectileRotationOffset);

            GameObject projectileObject = Instantiate(
                bouncyProjectilePrefab,
                resolvedFirePoint.position,
                visualRotation
            );

            // 이제 Size 값이 없어도 기본 크기(1)가 보장되어 무조건 화면에 보입니다.
            projectileObject.transform.localScale = Vector3.one * projectileSize;

            BouncyProjectile projectile = projectileObject.GetComponent<BouncyProjectile>();

            if (projectile == null)
            {
                Debug.LogWarning("[BouncyProjectileWeapon] BouncyProjectile 컴포넌트 없음");
                continue;
            }

            Vector3 startVelocity = shootRotation * Vector3.forward * projectileSpeed;
            startVelocity.y += upwardForce;

            projectile.Initialize(
                damageInfo,
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