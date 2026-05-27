using UnityEngine;

public class HomingWeapon : WeaponBase
{
    //Damage = 데미지
    //Cooldown = 쿨타임
    //ProjectileSpeed  = 속도
    //ProjectileCount = 개수
    //SpreadAngle  = 각도
    //Range = 최대 이동 거리
    //Radius = 탐지 반경

    [Header("Projectile References")]
    public GameObject homingProjectilePrefab;
    public ParticleSystem shootParticlePrefab;

    [Header("Fire Point")]
    public Transform firePoint;

    [Header("Rotation")]
    public Vector3 projectileRotationOffset;

    protected override void Attack(WeaponLevelData data)
    {
        if (data == null)
            return;

        FireProjectiles(
            GetFinalDamage(data.Damage),
            GetProjectileSpeed(data),
            Mathf.Max(
                1,
                GetFinalProjectileCount(data.ProjectileCount)
            ),
            data.SpreadAngle,
            GetFinalWeaponScale(data.Range),
            GetFinalWeaponScale(data.Radius),
            data.PierceCount
        );
    }

    private void FireProjectiles(
        DamageInfo damageInfo,
        float projectileSpeed,
        int count,
        float spreadAngleValue,
        float maxTravelDistance,
        float detectRadius,
        int pierceCount
    )
    {
        Transform resolvedFirePoint = ResolveFirePoint(firePoint);

        if (homingProjectilePrefab == null)
        {
            Debug.LogWarning("[HomingWeapon] Projectile Prefab 없음");
            return;
        }

        if (resolvedFirePoint == null)
        {
            Debug.LogWarning("[HomingWeapon] Fire Point 없음");
            return;
        }

        Collider targetCollider = FindClosestTarget(detectRadius);
        Transform targetTransform = targetCollider != null ? targetCollider.transform : null;

        Vector3 direction;

        if (targetCollider != null)
        {
            direction = targetCollider.bounds.center - resolvedFirePoint.position;
        }
        else
        {
            Debug.Log($"[HomingWeapon] 반경 {detectRadius} 내 적 없음 → 정면 발사");
            direction = resolvedFirePoint.forward;
        }

        Quaternion baseRotation = Quaternion.LookRotation(direction.normalized);

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

            // [수정됨] 대각선 위로 쏘아 올리던 코드를 제거하고, 원래대로 타겟을 향한 직사 방향에 좌우 퍼짐 각도(angle)만 적용합니다.
            Quaternion shootRotation =
                baseRotation *
                Quaternion.Euler(0f, angle, 0f);

            Quaternion visualRotation =
                shootRotation *
                Quaternion.Euler(projectileRotationOffset);

            GameObject projectileObject =
                Instantiate(
                    homingProjectilePrefab,
                    resolvedFirePoint.position,
                    visualRotation
                );

            HomingProjectile projectile =
                projectileObject.GetComponent<HomingProjectile>();

            if (projectile == null)
                continue;

            projectile.Initialize(
                damageInfo,
                projectileSpeed,
                maxTravelDistance,
                shootRotation * Vector3.forward,
                targetTransform,
                detectRadius,
                pierceCount
            );
        }
    }

    private Collider FindClosestTarget(float detectRadius)
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectRadius
        );

        Collider closestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
                continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null)
                continue;

            float distance = (hit.transform.position - transform.position).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = hit;
            }
        }

        return closestTarget;
    }

    private float GetProjectileSpeed(WeaponLevelData data)
    {
        return data.ProjectileSpeed > 0f
            ? data.ProjectileSpeed
            : data.BulletSpeed;
    }
}