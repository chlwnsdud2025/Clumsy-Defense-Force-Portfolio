using UnityEngine;

public class WD_BearShower : MeteorWeapon
{
    protected override void Attack(WeaponLevelData data)
    {
        if (data == null)
            return;

        int bearCount = Mathf.Max(1, GetFinalProjectileCount(data.ProjectileCount));

        for (int i = 0; i < bearCount; i++)
        {
            SpawnMeteor(
                GetFinalDamage(data.Damage),
                GetMeteorSpeed(data),
                GetFinalWeaponScale(data.ExplosionRadius),
                data.MeteorDelay,
                GetFinalWeaponScale(data.Radius),
                data.Range
            );
        }
    }
}
