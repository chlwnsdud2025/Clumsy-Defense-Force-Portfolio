using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyPoolData", menuName = "Survivor/Enemy Pool Data")]
public class EnemyPoolDataSO : ScriptableObject
{
    public GameObject prefab;

    [Header("Pool Settings")]
    public int poolSize = 50;
    public int maxReserveSize = 20;

    // 에디터에는 안 보이지만 런타임 중 동적 생성 개수를 추적
    [HideInInspector] public int currentReserveCount = 0;
}