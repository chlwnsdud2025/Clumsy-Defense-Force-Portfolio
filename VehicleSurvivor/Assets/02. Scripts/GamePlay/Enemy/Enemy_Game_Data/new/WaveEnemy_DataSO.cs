using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaveEnemy_Data
{
    public EnemyPoolDataSO targetPool;
    public float spawnWeight = 1f;
    public bool spawnAsPurePack = false;
}

[CreateAssetMenu(fileName = "NewWave_Data", menuName = "Survivor/WaveEnemy_DataSO")]
public class WaveEnemy_DataSO : ScriptableObject
{
    [Tooltip("해당 페이즈/악몽 진입 후 몇 초 뒤에 이 웨이브가 발동할지")]
    public float waveStartTime;
    public float spawnInterval = 2.0f;
    public int minSpawnCount = 3;
    public int maxSpawnCount = 7;
    public List<WaveEnemy_Data> activeEnemies;

    [Header("Mini-Boss Spawn (Wave Spike)")]
    [Tooltip("이 웨이브가 시작될 때 1회성으로 즉시 스폰할 '미니 보스' 프리팹들")]
    public List<GameObject> miniBossPrefabs;

    [Tooltip("몇 마리의 미니 보스를 한 번에 소환할지")]
    public int miniBossCount = 1;
}