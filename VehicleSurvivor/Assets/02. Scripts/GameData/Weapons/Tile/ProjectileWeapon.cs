using UnityEngine;

public class ProjectileWeapon : WeaponBase
{

   //Damage = 데미지
  // Cooldown = 발사 간격
  // ProjectileSpeed 또는 BulletSpeed = 당근 속도
  // ProjectileCount = 당근 개수
  // SpreadAngle = 여러 발일 때 퍼짐 각도
  // PierceCount = 관통 횟수
  // Radius = 타겟 탐지 반경
  // Range = 투사체 최대 이동 거리

    [Header("Projectile References")]
    public GameObject projectilePrefab;
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
            Mathf.Max(1, GetFinalProjectileCount(data.ProjectileCount)),
            data.SpreadAngle,
            data.Range,
            data.Radius,
            data.PierceCount
        );
    }

    private void FireProjectiles(
        float finalDamage,
        float projectileSpeed,
        int count,
        float spreadAngleValue,
        float maxTravelDistance,
        float detectRadius,
        int pierceCount
    )
    {
        Transform resolvedFirePoint = ResolveFirePoint(firePoint);

        if (projectilePrefab == null)
        {
            Debug.LogWarning("[ProjectileWeapon] Projectile Prefab이 비어있습니다.");
            return;
        }

        if (resolvedFirePoint == null)
        {
            Debug.LogWarning("[ProjectileWeapon] Fire Point가 비어있습니다.");
            return;
        }

        Transform target = FindClosestTarget(detectRadius);

        Vector3 direction;

        if (target != null)
        {
            direction = target.position - resolvedFirePoint.position;
            direction.y = 0f;
        }
        else
        {
            Debug.Log($"[ProjectileWeapon] 반경 {detectRadius} 내 IDamageable 대상 없음 → 정면 발사");

            direction = resolvedFirePoint.forward;
            direction.y = 0f;
        }

        if (direction == Vector3.zero)
            return;

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

            Quaternion shootRotation =
                baseRotation * Quaternion.Euler(0f, angle, 0f);

            Quaternion visualRotation =
                shootRotation * Quaternion.Euler(projectileRotationOffset);

            GameObject projectileObject = Instantiate(
                projectilePrefab,
                resolvedFirePoint.position,
                visualRotation
            );

            ProjectileBullet projectileBullet =
                projectileObject.GetComponent<ProjectileBullet>();

            if (projectileBullet == null)
            {
                Debug.LogWarning("[ProjectileWeapon] ProjectileBullet 컴포넌트가 없습니다.");
                continue;
            }

            projectileBullet.Initialize(
                finalDamage,
                projectileSpeed,
                pierceCount,
                maxTravelDistance,
                shootRotation * Vector3.forward
            );
        }
    }

    private Transform FindClosestTarget(float detectRadius)
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

    private float GetProjectileSpeed(WeaponLevelData data)
    {
        return data.ProjectileSpeed > 0f
            ? data.ProjectileSpeed
            : data.BulletSpeed;
    }
}