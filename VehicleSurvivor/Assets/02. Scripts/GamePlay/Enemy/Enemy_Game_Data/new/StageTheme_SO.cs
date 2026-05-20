using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StagePhase
{
    public float timeLimit = 300f; // 제한 시간
    public List<WaveEnemy_DataSO> phaseWaves; // 정상 스케줄
    public List<WaveEnemy_DataSO> nightmareWaves; // 시간 초과 시 악몽 스케줄
}

[CreateAssetMenu(fileName = "NewStage_Theme", menuName = "Survivor/Stage_Theme")]
public class StageTheme_SO : ScriptableObject
{
    public string themeName;
    public List<EnemyPoolDataSO> enemyPools;
    public List<StagePhase> phases; // 0~6페이즈 할당
}