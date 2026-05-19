using UnityEngine;

public class HomingProjectileWeapon : WeaponBase
{
    [Header("유도탄 설정")]
    public GameObject homingBulletPrefab;
    public Transform firePoint;

    protected override void Attack(WeaponLevelData data)
    {
        if (homingBulletPrefab == null || firePoint == null)
            return;

        int count = Mathf.Max(1, GetFinalProjectileCount(data.ProjectileCount));

        for (int i = 0; i < count; i++)
        {
            GameObject bullet = Instantiate(
                homingBulletPrefab,
                firePoint.position,
                firePoint.rotation
            );

            HomingProjectileBullet homingBullet =
                bullet.GetComponent<HomingProjectileBullet>();

            if (homingBullet != null)
            {
                homingBullet.Init(
                    GetFinalDamage(data.Damage),
                    data.bulletSpeed
                );
            }
        }
    }
}
