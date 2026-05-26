using UnityEngine;

public class TwinBoss_Geezer : BossEnemy
{
    protected override void Start()
    {
        base.Start();
    }

    protected override void Attack()
    {
    }

    protected override void HandleStageClear()
    {
        TwinBossManager manager = GetComponentInParent<TwinBossManager>();
        if (manager != null)
        {
            manager.OnTwinDied(); 
        }
    }
}
