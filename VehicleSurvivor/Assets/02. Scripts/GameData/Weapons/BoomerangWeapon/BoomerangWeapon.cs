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

        GameObject boomerang = Instantiate(
            boomerangPrefab,
            resolvedFirePoint.position,
            resolvedFirePoint.rotation
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
