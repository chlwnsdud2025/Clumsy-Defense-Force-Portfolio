using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStage_Theme", menuName = "Survivor/Stage_Theme")]
public class StageTheme_SO : ScriptableObject
{
    public string themeName;
    public List<EnemyPoolDataSO> enemyPools;
    public List<StagePhase_SO> phases; // 0~6페이즈 할당 (StagePhase_SO 순서대로)
}
