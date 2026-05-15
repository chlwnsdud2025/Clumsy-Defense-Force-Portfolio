using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaveEnemyData
{
    [Tooltip("스폰할 몹의 풀 데이터를 직접 끌어다 놓으세요.")]
    public EnemyPoolDataSO targetPool;

    [Tooltip("스폰 룰렛에서 차지할 가중치 (비율)")]
    public float spawnWeight = 1f;

    [Tooltip("체크 시 이 몹이 당첨됐을 때 섞이지 않고 단일 몹 군집으로 나옵니다.")]
    public bool spawnAsPurePack = false;
}

[CreateAssetMenu(fileName = "NewWaveData", menuName = "Survivor/Wave Data")]
public class WaveDataSO : ScriptableObject 
{
    [Header("Wave Trigger")]
    [Tooltip("게임 시작 후 이 시간(초)이 지나면 해당 웨이브가 시작됩니다.")]
    public float waveStartTime;

    [Header("Spawn Settings")]
    public float spawnInterval = 2.0f;
    public int minSpawnCount = 3;
    public int maxSpawnCount = 7;

    [Header("Enemy Roster")]
    [Tooltip("이 시간대에 스폰될 적의 종류와 확률 세팅")]
    public List<WaveEnemyData> activeEnemies;

    [Header("Mid-Boss Spawn (Direct Instantiate)")]
    [Tooltip("소환할 보스 프리팹들을 직접 넣어주세요.")]
    public List<GameObject> midBossPrefabs;

    [Tooltip("몇 마리를 소환할지 정합니다.")]
    public int midBossCount = 1;
}