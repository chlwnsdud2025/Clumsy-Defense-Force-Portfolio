using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStageTheme", menuName = "Survivor/Stage Theme")]
public class StageThemeData : ScriptableObject
{
    public string themeName;

    [Header("Pooling Settings")]
    [Tooltip("이 스테이지에서 사용되는 모든 몹의 풀 정보를 넣어주세요.")]
    public List<EnemyPoolDataSO> enemyPools;

    [Header("Wave Schedule")]
    [Tooltip("이 스테이지의 시간대별 스폰 규칙(SO)들을 넣어주세요.")]
    public List<WaveDataSO> waveDatas;
}