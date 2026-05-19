using UnityEngine;

public class ProjectileWeapon : WeaponBase
{
    [Header("투사체 데이터")]
    public ProjectileWeaponDataSO projectileData;

    [Header("발사 위치")]
    public Transform firePoint;

    [Header("임시 테스트 스탯")]
    public float damage = 10f;
    public float cooldown = 1f;
    public float bulletSpeed = 20f;
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
        if (projectileData == null)
        {
            Debug.LogWarning("Projectile Data가 비어있습니다.");
            return;
        }

        if (projectileData.projectilePrefab == null)
        {
            Debug.LogWarning("Projectile Prefab이 비어있습니다.");
            return;
        }

        if (firePoint == null)
        {
            Debug.LogWarning("Fire Point가 비어있습니다.");
            return;
        }

        if (projectileData.shootParticlePrefab != null)
        {
            Instantiate(
                projectileData.shootParticlePrefab,
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
                float centerIndex =
                    (count - 1) * 0.5f;

                angle =
                    (i - centerIndex)
                    * spreadAngle;
            }

            Quaternion bulletRotation =
                firePoint.rotation *
                Quaternion.Euler(
                    0f,
                    angle,
                    0f
                );

            GameObject bulletObject =
                Instantiate(
                    projectileData.projectilePrefab,
                    firePoint.position,
                    bulletRotation
                );

            ProjectileBullet bullet =
                bulletObject.GetComponent<ProjectileBullet>();

            if (bullet != null)
            {
                bullet.Init(
                    GetFinalDamage(damage),
                    bulletSpeed,
                    bulletRotation * Vector3.forward
                );
            }
        }
    }
}