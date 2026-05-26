using UnityEngine;

public class MushRoom_Middle_Boss : BossEnemy
{
    protected override void Attack()
    {
        // 아무것도 하지 않음
    }

    protected override void HandleStageClear()
    {
       Debug.Log($"{gameObject.name} (미니 보스) 처치 완료! 웨이브 계속 진행.");

    }
}
