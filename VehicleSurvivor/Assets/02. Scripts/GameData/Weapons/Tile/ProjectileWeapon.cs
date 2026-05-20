using UnityEngine;

public class ProjectileWeapon : WeaponBase
{
    [Header("Projectile References")]
    public GameObject projectilePrefab;
    public ParticleSystem shootParticlePrefab;

    [Header("Fire Point")]
    public Transform firePoint;

    [Header("Legacy Stats")]
    public float damage = 10f;
    public float cooldown = 1f;
    public float bulletSpeed = 20f;
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
        FireProjectiles(
            GetFinalDamage(data.Damage),
            GetProjectileSpeed(data),
            Mathf.Max(1, GetFinalProjectileCount(data.ProjectileCount)),
            data.spreadAngle
        );
    }

    private void AttackLegacy()
    {
        FireProjectiles(
            GetFinalDamage(damage),
            bulletSpeed,
            Mathf.Max(1, GetFinalProjectileCount(projectileCount)),
            spreadAngle
        );
    }

    private void FireProjectiles(
        float finalDamage,
        float projectileSpeed,
        int count,
        float spreadAngleValue
    )
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("Projectile prefab is missing.");
            return;
        }

        if (firePoint == null)
        {
            Debug.LogWarning("Fire point is missing.");
            return;
        }

        if (shootParticlePrefab != null)
        {
            Instantiate(
                shootParticlePrefab,
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

            Quaternion bulletRotation =
                firePoint.rotation *
                Quaternion.Euler(0f, angle, 0f);

            GameObject bulletObject = Instantiate(
                projectilePrefab,
                firePoint.position,
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
        if (data == null)
            return bulletSpeed;

        return data.ProjectileSpeed > 0f ? data.ProjectileSpeed : data.bulletSpeed;
    }
}
