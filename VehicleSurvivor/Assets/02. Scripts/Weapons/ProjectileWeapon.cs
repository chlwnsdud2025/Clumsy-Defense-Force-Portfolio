using UnityEngine;

public class ProjectileWeapon : WeaponBase
{
    [Header("발사 위치")]
    public Transform firePoint;

    [Header("총알 프리팹")]
    public GameObject projectilePrefab;

    protected override void Attack()
    {
        WeaponLevelData data = GetCurrentLevelData();

        if (data == null)
            return;

        int projectileCount = data.projectileCount;

        float angleStep = 10f;
        float startAngle = -(angleStep * (projectileCount - 1)) / 2f;

        for (int i = 0; i < projectileCount; i++)
        {
            float currentAngle = startAngle + (angleStep * i);

            Quaternion rotation =
                firePoint.rotation *
                Quaternion.Euler(0f, currentAngle, 0f);

            GameObject bullet = Instantiate(
                projectilePrefab,
                firePoint.position,
                rotation
            );

            ProjectileBullet projectile =
                bullet.GetComponent<ProjectileBullet>();

            if (projectile != null)
            {
                projectile.Setup(data);
            }

            if (data.attackParticlePrefab != null)
            {
                Instantiate(
                    data.attackParticlePrefab,
                    firePoint.position,
                    rotation
                );
            }
        }
    }
}