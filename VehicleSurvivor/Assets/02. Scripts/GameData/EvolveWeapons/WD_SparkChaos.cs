using UnityEngine;

public class WD_SparkChaos : ChainLightningWeapon
{
    protected override void Attack(WeaponLevelData data)
    {
        if (data == null)
            return;

        int dischargeCount = Mathf.Max(1, GetFinalProjectileCount(data.ProjectileCount));

        for (int i = 0; i < dischargeCount; i++)
        {
            base.Attack(data);
        }
    }
}
