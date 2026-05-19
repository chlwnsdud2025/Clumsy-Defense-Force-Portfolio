using UnityEngine;

public class HomingWeapon : WeaponBase
{
    [Header("유도 무기 데이터")]
    public HomingWeaponDataSO homingData;

    [Header("발사 위치")]
    public Transform firePoint;

    [Header("레이어 설정")]
    public LayerMask targetLayer;

    [Header("임시 테스트 스탯")]
    public float damage = 10f;
    public float cooldown = 1f;
    public float bulletSpeed = 15f;
    public float detectRadius = 15f;
    public int projectileCount = 1;
    public float spreadAngle = 15f;

    private float attackTimer;

    private void Update()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer >= GetFinalCooldown(cooldown))
        {
            Attack();
            attackTimer = 0f;
        }
    }

    private void Attack()
    {
        if (homingData == null || homingData.homingProjectilePrefab == null)
            return;

        if (firePoint == null)
            return;

        Transform target = FindClosestTarget();

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

        int count = Mathf.Max(
            1,
            GetFinalProjectileCount(projectileCount)
        );

        for (int i = 0; i < count; i++)
        {
            float angle = 0f;

            if (count > 1)
            {
                float centerIndex = (count - 1) * 0.5f;
                angle = (i - centerIndex) * spreadAngle;
            }

            Quaternion rotation =
                firePoint.rotation * Quaternion.Euler(0f, angle, 0f);

            GameObject projectile = Instantiate(
                homingData.homingProjectilePrefab,
                firePoint.position,
                rotation
            );

            HomingProjectile homing =
                projectile.GetComponent<HomingProjectile>();

            if (homing != null)
            {
                homing.Init(
                    target,
                    GetFinalDamage(damage),
                    bulletSpeed,
                    targetLayer,
                    homingData.hitEffectPrefab
                );
            }
        }
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
            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            Transform targetTransform = hit.transform;

            float distance =
                (targetTransform.position - transform.position).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = targetTransform;
            }
        }

        return closestTarget;
    }
}