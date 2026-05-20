using UnityEngine;

public class BouncyProjectileWeapon : WeaponBase
{
    [Header("탱탱볼 설정")]
    public GameObject bouncyProjectilePrefab;
    public Transform firePoint;

    [Header("레이어 설정")]
    public LayerMask enemyLayer;
    public LayerMask bounceLayer;

    protected override void Attack(WeaponLevelData data)
    {
        if (bouncyProjectilePrefab == null || firePoint == null)
            return;

        int count = Mathf.Max(1, GetFinalProjectileCount(data.ProjectileCount));

        for (int i = 0; i < count; i++)
        {
            float angle = 0f;

            if (count > 1)
            {
                float centerIndex = (count - 1) * 0.5f;
                angle = (i - centerIndex) * data.spreadAngle;
            }

            Quaternion rotation =
                firePoint.rotation * Quaternion.Euler(0f, angle, 0f);

            GameObject ball = Instantiate(
                bouncyProjectilePrefab,
                firePoint.position,
                rotation
            );

            BouncyProjectile projectile =
                ball.GetComponent<BouncyProjectile>();

            if (projectile != null)
            {
                projectile.Init(
                    GetFinalDamage(data.Damage),
                    data.bulletSpeed,
                    data.bounceCount,
                    enemyLayer,
                    bounceLayer,
                    rotation * Vector3.forward
                );
            }
        }
    }
}
