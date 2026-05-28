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

        // [수정됨] Transform 대신 Collider를 반환받아 정확한 크기와 중심을 알아냅니다.
        Collider targetCollider = FindClosestTarget(detectRadius);

        Vector3 direction;

        if (targetCollider != null)
        {
            // [수정됨] 몬스터 콜라이더의 정확한 중심(Center)을 조준합니다.
            Vector3 targetCenter = targetCollider.bounds.center;
            direction = targetCenter - resolvedFirePoint.position;

            // direction.y = 0f; <--- 삭제됨: Y축(높이차)을 무시하지 않고 3D로 직접 겨냥합니다.
        }
        else
        {
            Debug.Log($"[ProjectileWeapon] 반경 {detectRadius} 내 Enemy 대상 없음 → 정면 발사");

            direction = resolvedFirePoint.forward;
            direction.y = 0f; // 타겟이 없을 때는 기존처럼 수평으로 발사
        }

        if (direction == Vector3.zero)
            return;

        // Quaternion.LookRotation이 Y축 방향(위/아래)까지 고려하여 자연스럽게 포신을 꺾어줍니다.
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
                float centerIndex =
                    (count - 1) * 0.5f;

                angle =
                    (i - centerIndex) *
                    spreadAngleValue;
            }

            // 부채꼴 퍼짐 각도 적용 (수평면 기준)
            Quaternion shootRotation =
                baseRotation *
                Quaternion.Euler(
                    0f,
                    angle,
                    0f
                );

            Quaternion visualRotation = // 프리팹별로 y값 설정할수있게 수정
      shootRotation *
      Quaternion.Euler(
          projectileRotationOffset
      );

            GameObject projectileObject =
                Instantiate(
                    projectilePrefab,
                    resolvedFirePoint.position,
                    visualRotation
                );

            ProjectileBullet projectileBullet =
                projectileObject.GetComponent<ProjectileBullet>();

            if (projectileBullet == null)
            {
                Debug.LogWarning(
                    "[ProjectileWeapon] ProjectileBullet 컴포넌트가 없습니다."
                );
                continue;
            }

            projectileBullet.Initialize(
                damageInfo,
                projectileSpeed,
                pierceCount,
                maxTravelDistance,
                shootRotation * Vector3.forward // 발사 방향도 정확한 3D 각도로 전달
            );
        }
    }

    // [수정됨] Transform 대신 Collider를 반환하도록 변경
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

            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            float distance =
                (hit.transform.position - transform.position).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = hit; // transform이 아닌 collider 자체를 캐싱
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