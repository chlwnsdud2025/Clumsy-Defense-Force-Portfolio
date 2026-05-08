using UnityEngine;

public class ProjectileWeapon : WeaponBase
{
    [Header("Projectile Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 20f;

    [Header("Multi Shot")]
    public int projectileCount = 1;
    public float spreadAngle = 15f;

    protected override void Attack()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogWarning("총알 프리팹 또는 FirePoint가 비어있습니다.");
            return;
        }

        projectileCount = Mathf.Max(1, projectileCount);

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = 0f;

            if (projectileCount > 1)
            {
                float centerIndex = (projectileCount - 1) / 2f;
                angle = (i - centerIndex) * spreadAngle;
            }

            Quaternion bulletRotation =
                firePoint.rotation * Quaternion.Euler(0f, angle, 0f);

            GameObject bullet = Instantiate(
                bulletPrefab,
                firePoint.position,
                bulletRotation
            );

            Rigidbody rb = bullet.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.linearVelocity = bulletRotation * Vector3.forward * bulletSpeed;
            }

            ProjectileBullet projectile = bullet.GetComponent<ProjectileBullet>();

            if (projectile != null)
            {
                projectile.damage = damage;
            }
        }
    }

    public override void LevelUp()
    {
        if (level >= maxLevel)
        {
            Debug.Log($"{weaponName}은 이미 최대 레벨입니다.");
            return;
        }

        level++;

        if (level == 2)
        {
            damage += 5f;
        }
        else if (level == 3)
        {
            cooldown *= 0.8f;
        }
        else if (level == 4)
        {
            projectileCount = 2;
        }
        else if (level == 5)
        {
            projectileCount = 3;
            spreadAngle = 15f;
        }

        bulletSpeed += 2f;

        Debug.Log($"{weaponName} Lv.{level} 강화 완료");
    }
}