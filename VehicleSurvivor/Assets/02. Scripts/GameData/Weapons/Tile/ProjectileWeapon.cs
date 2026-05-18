using UnityEngine;

public class ProjectileWeapon : WeaponBase
{
    [Header("투사체 설정")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("이펙트 설정")]
    public ParticleSystem shootParticle;

    protected override void Attack(WeaponLevelData data)
    {
        if (bulletPrefab == null || firePoint == null)
            return;

        if (shootParticle != null)
        {
            shootParticle.Play();
        }

        int count = Mathf.Max(1, GetFinalProjectileCount(data.projectileCount));

        for (int i = 0; i < count; i++)
        {
            float angle = 0f;

            if (count > 1)
            {
                float centerIndex = (count - 1) * 0.5f;
                angle = (i - centerIndex) * data.spreadAngle;
            }

            Quaternion bulletRotation =
                firePoint.rotation * Quaternion.Euler(0f, angle, 0f);

            GameObject bullet = Instantiate(
                bulletPrefab,
                firePoint.position,
                bulletRotation
            );

            ProjectileBullet projectileBullet =
                bullet.GetComponent<ProjectileBullet>();

            if (projectileBullet != null)
            {
                projectileBullet.Init(
                    GetFinalDamage(data.damage),
                    data.bulletSpeed,
                    bulletRotation * Vector3.forward
                );
            }
            else
            {
                Rigidbody rb = bullet.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    rb.linearVelocity =
                        bulletRotation * Vector3.forward * data.bulletSpeed;
                }
            }
        }
    }
}