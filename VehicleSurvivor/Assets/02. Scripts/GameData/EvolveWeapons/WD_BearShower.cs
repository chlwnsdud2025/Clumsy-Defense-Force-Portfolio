using UnityEngine;

public class WD_BearShower : MeteorWeapon
{
    protected override void Attack(WeaponLevelData data)
    {
        if (data == null)
            return;

        SpawnMeteor(
            GetFinalDamage(data.Damage),
            GetMeteorSpeed(data),
            GetFinalWeaponScale(data.ExplosionRadius),
            data.MeteorDelay,
            GetFinalWeaponScale(data.Radius),
            data.Range,
            Mathf.Max(1, GetFinalProjectileCount(data.ProjectileCount))
        );
    }
}
