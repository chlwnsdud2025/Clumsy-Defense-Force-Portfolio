using UnityEngine;

public class BoomerangWeapon : WeaponBase
{
    [Header("부메랑 설정")]
    public GameObject boomerangPrefab;
    public Transform firePoint;

    [Header("레이어 설정")]
    public LayerMask enemyLayer;

    protected override void Attack(WeaponLevelData data)
    {
        Transform resolvedFirePoint = ResolveFirePoint(firePoint);

        if (boomerangPrefab == null || resolvedFirePoint == null)
            return;

        int count = Mathf.Max(1, GetFinalProjectileCount(data.ProjectileCount));
        float spreadAngle = data.SpreadAngle;

        for (int i = 0; i < count; i++)
        {
            float angle = 0f;
            if (count > 1)
            {
                float centerIndex = (count - 1) * 0.5f;
                angle = (i - centerIndex) * spreadAngle;
            }

            SpawnBoomerang(data, resolvedFirePoint, angle);
        }
    }

    private void SpawnBoomerang(WeaponLevelData data, Transform resolvedFirePoint, float angle)
    {
        Quaternion rotation = resolvedFirePoint.rotation * Quaternion.Euler(0f, angle, 0f);

        GameObject boomerang = Instantiate(
            boomerangPrefab,
            resolvedFirePoint.position,
            rotation
        );

        BoomerangProjectile projectile =
            boomerang.GetComponent<BoomerangProjectile>();

        if (projectile != null)
        {
            projectile.Init(
                transform,
                GetFinalDamage(data.Damage),
                data.BulletSpeed,
                GetFinalWeaponScale(data.Radius),
                enemyLayer
            );
        }
    }
}
