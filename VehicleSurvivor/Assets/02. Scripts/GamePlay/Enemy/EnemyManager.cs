using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyPoolSetting
{
    public EnemyType type;
    public GameObject prefab;
    public int poolSize = 50;
    public float amountRate = 1f;

    [Header("Cluster Settings")]
    [Tooltip("체크하면 이 몹이 당첨됐을 때 섞이지 않고 단일 몹 군집으로 나옵니다.")]
    public bool spawnAsPurePack = false;

    [Header("Dynamic Reserve Pool")]
    [Tooltip("기본 풀이 고갈되었을 때, 추가로 생성(Instantiate)을 허락할 최대 예비 개수")]
    public int maxReserveSize = 20;

    // 에디터에는 안 보이지만 내부적으로 추가 생성된 개수를 추적하는 변수
    [HideInInspector] public int currentReserveCount = 0;
}

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("References")]
    public Transform playerTransform;

    [Header("Pool Settings")]
    public EnemyPoolSetting[] poolSettings;

    [Header("Cluster Spawn Settings")]
    public float spawnRadius = 50f;
    public float spawnInterval = 2.0f;
    public float clusterRadius = 4f;
    public int minSpawnCount = 3;
    public int maxSpawnCount = 7;
    private float spawnTimer = 0f;

    [Header("3D Spawn Settings")]
    public LayerMask groundLayer;
    public float raycastStartHeight = 100f;
    public float raycastDistance = 200f;

    [Header("Grid & Separation Settings")]
    public float cellSize = 2f;
    private Dictionary<Vector2Int, List<Enemy>> spatialGrid = new Dictionary<Vector2Int, List<Enemy>>();

    // ==========================================
    // [최적화 추가] 그리드 업데이트 타이머 변수
    // ==========================================
    private float gridUpdateTimer = 0f;
    public float gridUpdateInterval = 0.15f; // 0.15초마다 한 번씩만 그리드 갱신 (1초에 약 6번)

    // 풀 관리용 Dictionary
    private Dictionary<EnemyType, Queue<Enemy>> enemyPools;
    private Dictionary<EnemyType, Transform> poolParents; // 예비 풀 생성 시 부모를 찾기 위한 딕셔너리
    private List<Enemy> activeEnemies = new List<Enemy>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        enemyPools = new Dictionary<EnemyType, Queue<Enemy>>();
        poolParents = new Dictionary<EnemyType, Transform>();

        foreach (var setting in poolSettings)
        {
            Queue<Enemy> pool = new Queue<Enemy>();
            GameObject poolParent = new GameObject($"{setting.type} Pool");
            poolParent.transform.SetParent(this.transform);

            poolParents.Add(setting.type, poolParent.transform);

            for (int i = 0; i < setting.poolSize; i++)
            {
                GameObject obj = Instantiate(setting.prefab, poolParent.transform);
                obj.SetActive(false);

                Enemy enemyScript = obj.GetComponent<Enemy>();
                enemyScript.myType = setting.type;

                pool.Enqueue(enemyScript);
            }

            enemyPools.Add(setting.type, pool);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // ==========================================
        // [최적화 적용] 그리드 갱신을 타이머로 분산
        // ==========================================
        gridUpdateTimer += dt;
        if (gridUpdateTimer >= gridUpdateInterval)
        {
            gridUpdateTimer = 0f;
            UpdateGrid();
        }

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            activeEnemies[i].ManagedUpdate(dt);
        }

        spawnTimer += dt;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnEnemyAtRandomCircle();
        }
    }

    // ==========================================
    // 확률 룰렛을 돌려 특정 몹의 세팅을 반환
    // ==========================================
    private EnemyPoolSetting GetRandomEnemySetting()
    {
        float totalRate = 0f;
        for (int i = 0; i < poolSettings.Length; i++) totalRate += poolSettings[i].amountRate;

        if (totalRate <= 0f) return null;

        float randomValue = Random.Range(0f, totalRate);
        float cumulativeRate = 0f;

        for (int i = 0; i < poolSettings.Length; i++)
        {
            cumulativeRate += poolSettings[i].amountRate;
            if (randomValue <= cumulativeRate)
            {
                return poolSettings[i];
            }
        }
        return poolSettings[0];
    }

    // ==========================================
    // 예비 풀(동적 확장)을 포함한 몹 꺼내기 로직
    // ==========================================
    private Enemy GetEnemyFromPool(EnemyType type)
    {
        if (enemyPools[type].Count > 0)
        {
            return enemyPools[type].Dequeue();
        }

        EnemyPoolSetting setting = null;
        foreach (var s in poolSettings) { if (s.type == type) { setting = s; break; } }

        if (setting != null && setting.currentReserveCount < setting.maxReserveSize)
        {
            setting.currentReserveCount++;

            GameObject obj = Instantiate(setting.prefab, poolParents[type]);
            obj.SetActive(false);

            Enemy enemyScript = obj.GetComponent<Enemy>();
            enemyScript.myType = type;

            Debug.LogWarning($"[동적 풀 확장] {type} 풀 고갈! 임시로 1개 추가 생성했습니다. (현재 추가량: {setting.currentReserveCount}/{setting.maxReserveSize})");
            return enemyScript;
        }

        return null;
    }

    // ==========================================
    // 단일 / 혼합 군집 스폰 로직
    // ==========================================
    private void SpawnEnemyAtRandomCircle()
    {
        if (poolSettings.Length == 0) return;

        EnemyPoolSetting clusterTheme = GetRandomEnemySetting();
        if (clusterTheme == null) return;

        bool isPurePack = clusterTheme.spawnAsPurePack;

        Vector2 centerCircle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 clusterCenterXZ = new Vector3(
            playerTransform.position.x + centerCircle.x,
            0f,
            playerTransform.position.z + centerCircle.y
        );

        int spawnCount = Random.Range(minSpawnCount, maxSpawnCount + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            EnemyPoolSetting targetSetting = isPurePack ? clusterTheme : GetRandomEnemySetting();
            Enemy enemyToSpawn = GetEnemyFromPool(targetSetting.type);

            if (enemyToSpawn == null) continue;

            Vector2 randomOffset = Random.insideUnitCircle * clusterRadius;
            Vector3 rayStartPos = new Vector3(
                clusterCenterXZ.x + randomOffset.x,
                playerTransform.position.y + raycastStartHeight,
                clusterCenterXZ.z + randomOffset.y
            );

            // 기본 Physics.Raycast는 단일 검사이며 GC를 발생시키지 않으므로 스폰 단계에서는 그대로 두어도 매우 안전합니다.
            if (Physics.Raycast(rayStartPos, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
            {
                enemyToSpawn.SpawnSetup(playerTransform, hit.point, this);
                activeEnemies.Add(enemyToSpawn);
            }
            else
            {
                ReturnToPool(enemyToSpawn);
            }
        }
    }

    public void ReturnToPool(Enemy enemy)
    {
        enemy.gameObject.SetActive(false);
        activeEnemies.Remove(enemy);
        enemyPools[enemy.myType].Enqueue(enemy);
    }

    private void UpdateGrid()
    {
        foreach (var list in spatialGrid.Values) list.Clear();

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            Enemy enemy = activeEnemies[i];
            Vector2Int cell = GetCellPos(enemy.transform.position);

            if (!spatialGrid.ContainsKey(cell)) spatialGrid[cell] = new List<Enemy>(10);
            spatialGrid[cell].Add(enemy);
        }
    }

    private Vector2Int GetCellPos(Vector3 position)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x / cellSize), Mathf.FloorToInt(position.z / cellSize));
    }

    // ==========================================
    // [최적화] 군집 분리 연산 제한
    // ==========================================
    public Vector3 GetSeparationForce(Enemy targetEnemy, float radius)
    {
        Vector3 force = Vector3.zero;
        Vector2Int cell = GetCellPos(targetEnemy.transform.position);
        float sqrRadius = radius * radius;

        int checkCount = 0;
        const int MAX_CHECKS = 5; //  한 마리당 최대 5마리와만 거리 계산을 수행합니다.

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int checkCell = new Vector2Int(cell.x + x, cell.y + y);

                if (spatialGrid.TryGetValue(checkCell, out List<Enemy> neighbors))
                {
                    for (int i = 0; i < neighbors.Count; i++)
                    {
                        if (checkCount >= MAX_CHECKS) return force; // 연산 한계치 도달 시 즉시 종료 (렉 방지)

                        Enemy neighbor = neighbors[i];
                        if (neighbor == targetEnemy) continue;

                        Vector3 diff = targetEnemy.transform.position - neighbor.transform.position;
                        diff.y = 0;
                        float sqrDist = diff.sqrMagnitude;

                        if (sqrDist > 0 && sqrDist < sqrRadius)
                        {
                            // 무거운 Sqrt 연산도 최대 5번만 실행됩니다.
                            force += diff.normalized / Mathf.Sqrt(sqrDist);
                            checkCount++;
                        }
                    }
                }
            }
        }
        return force;
    }
}