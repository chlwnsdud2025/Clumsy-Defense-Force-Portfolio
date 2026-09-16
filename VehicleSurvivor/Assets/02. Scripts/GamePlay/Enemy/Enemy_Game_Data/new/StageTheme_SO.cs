using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStage_Theme", menuName = "Survivor/Stage_Theme")]
public class StageTheme_SO : ScriptableObject
{
    public string themeName;
    public List<StagePhase_SO> phases; // 0~6페이즈 할당 (StagePhase_SO 순서대로)

    /// <summary>
    /// 이 테마의 모든 페이즈 → 웨이브 → activeEnemies 를 훑어 실제로 사용되는 풀을 중복 없이 모은다.
    /// 풀 목록을 손으로 관리하지 않고 웨이브 데이터에서 자동으로 도출한다.
    /// </summary>
    public List<EnemyPoolDataSO> CollectEnemyPools()
    {
        List<EnemyPoolDataSO> result = new List<EnemyPoolDataSO>();
        HashSet<EnemyPoolDataSO> seen = new HashSet<EnemyPoolDataSO>();

        if (phases == null) return result;

        foreach (StagePhase_SO phase in phases)
        {
            if (phase == null) continue;
            Collect(phase.phaseWaves, result, seen);
            Collect(phase.nightmareWaves, result, seen);
        }

        return result;
    }

    private static void Collect(List<WaveEnemy_DataSO> waves, List<EnemyPoolDataSO> result, HashSet<EnemyPoolDataSO> seen)
    {
        if (waves == null) return;

        foreach (WaveEnemy_DataSO wave in waves)
        {
            if (wave == null || wave.activeEnemies == null) continue;

            foreach (WaveEnemy_Data entry in wave.activeEnemies)
            {
                EnemyPoolDataSO pool = entry?.targetPool;
                if (pool != null && seen.Add(pool))
                    result.Add(pool);
            }
        }
    }
}
