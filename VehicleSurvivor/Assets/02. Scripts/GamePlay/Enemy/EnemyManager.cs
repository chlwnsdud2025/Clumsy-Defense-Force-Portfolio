using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("Current Stage Setting")]
    [Tooltip("현재 맵에 해당하는 테마 SO를 할당하세요.")]
    public StageThemeSO currentTheme;

    [Header("References")]
    public Transform playerTransform;

    [Header("Cluster Spawn Settings")]
    public float spawnRadius = 50f;
    public float clusterRadius = 4f;

    [Header("3D Spawn Settings")]
    public LayerMask groundLayer;
    public float raycastStartHeight = 100f;
    public float raycastDistance = 200f;

    [Header("Grid & Separation Settings")]
    public float cellSize = 2f;
    public float gridUpdateInterval = 0.15f;

    // 내부 타이머 및 상태
    private float spawnTimer = 0f;
    private float gameTimer = 0f;
    private float gridUpdateTimer = 0f;

    // ==========================================
    // [변경됨] 데이터 관리 컬렉션: Enum 대신 SO 자체를 Key로 사용합니다.
    // ==========================================
    private Dictionary<Vector2Int, List<Enemy>> spatialGrid = new Dictionary<Vector2Int, List<Enemy>>();
    private Dictionary<EnemyPoolDataSO, Queue<Enemy>> enemyPools = new Dictionary<EnemyPoolDataSO, Queue<Enemy>>();
    private Dictionary<EnemyPoolDataSO, Transform> poolParents = new Dictionary<EnemyPoolDataSO, Transform>();
    // 참고: SO 자체가 Key이므로 이제 poolSettings 딕셔너리는 필요 없습니다.
    private List<Enemy> activeEnemies = new List<Enemy>();

    private List<WaveDataSO> sortedWaves;
    private WaveDataSO currentWave;
    private int currentWaveIndex = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (currentTheme != null)
        {
            ApplyTheme(currentTheme);
        }
    }

    public void ApplyTheme(StageThemeSO theme)
    {
        currentTheme = theme;
        gameTimer = 0f; // 테마 적용 시 게임 시간 초기화

        ClearAllPools();

        // 풀 생성
        foreach (EnemyPoolDataSO poolData in theme.enemyPools)
        {
            if (poolData == null) continue;

            poolData.currentReserveCount = 0; // 예비 카운트 초기화

            Queue<Enemy> pool = new Queue<Enemy>();
            GameObject parentObj = new GameObject($"{poolData.name}_Pool"); // SO의 파일 이름을 부모 객체 이름으로 사용
            parentObj.transform.SetParent(this.transform);

            // Enum 대신 SO 객체를 Key로 등록
            poolParents.Add(poolData, parentObj.transform);

            for (int i = 0; i < poolData.poolSize; i++)
            {
                Enemy enemy = InstantiateEnemy(poolData, parentObj.transform);
                pool.Enqueue(enemy);
            }

            enemyPools.Add(poolData, pool);
        }

        // 웨이브 스케줄 정렬
        sortedWaves = theme.waveDatas.OrderBy(w => w.waveStartTime).ToList();
        if (sortedWaves.Count > 0)
        {
            currentWave = sortedWaves[0];
            currentWaveIndex = 0;
        }

        Debug.Log($"[Theme Applied] {theme.themeName} 테마 로딩 완료");
    }

    private Enemy InstantiateEnemy(EnemyPoolDataSO data, Transform parent)
    {
        GameObject obj = Instantiate(data.prefab, parent);
        obj.SetActive(false);
        Enemy enemy = obj.GetComponent<Enemy>();

        // [변경됨] Enum 대신 자신이 태어난 SO(풀 데이터)를 기억하게 합니다.
        enemy.originPool = data;

        return enemy;
    }

    private void ClearAllPools()
    {
        foreach (var parent in poolParents.Values) Destroy(parent.gameObject);
        enemyPools.Clear();
        poolParents.Clear();
        activeEnemies.Clear();
        spatialGrid.Clear();
    }

    void Update()
    {
        if (currentWave == null || playerTransform == null) return;

        float dt = Time.deltaTime;
        gameTimer += dt;

        // 1. 웨이브 교체 체크
        if (currentWaveIndex + 1 < sortedWaves.Count)
        {
            if (gameTimer >= sortedWaves[currentWaveIndex + 1].waveStartTime)
            {
                currentWaveIndex++;
                currentWave = sortedWaves[currentWaveIndex];
                Debug.Log($"[Wave Changed] 새로운 웨이브 시작!");
            }
        }

        // 2. 그리드 갱신 (최적화)
        gridUpdateTimer += dt;
        if (gridUpdateTimer >= gridUpdateInterval)
        {
            gridUpdateTimer = 0f;
            UpdateGrid();
        }

        // 3. 활성화된 몹 업데이트
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            activeEnemies[i].ManagedUpdate(dt);
        }

        // 4. 스폰 타이머
        spawnTimer += dt;
        if (spawnTimer >= currentWave.spawnInterval)
        {
            spawnTimer = 0f;
            SpawnEnemyAtRandomCircle();
        }
    }

    private WaveEnemyData GetRandomEnemyDataFromWave()
    {
        if (currentWave.activeEnemies.Count == 0) return null;

        float totalWeight = 0f;
        for (int i = 0; i < currentWave.activeEnemies.Count; i++) totalWeight += currentWave.activeEnemies[i].spawnWeight;

        if (totalWeight <= 0f) return null;

        float randomValue = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;

        for (int i = 0; i < currentWave.activeEnemies.Count; i++)
        {
            cumulativeWeight += currentWave.activeEnemies[i].spawnWeight;
            if (randomValue <= cumulativeWeight) return currentWave.activeEnemies[i];
        }
        return currentWave.activeEnemies[0];
    }

    // [변경됨] 매개변수가 Enum이 아닌 SO 객체 자체를 받습니다.
    private Enemy GetEnemyFromPool(EnemyPoolDataSO poolData)
    {
        if (poolData == null) return null;

        if (enemyPools.ContainsKey(poolData) && enemyPools[poolData].Count > 0)
        {
            return enemyPools[poolData].Dequeue();
        }

        // 동적 풀 확장 (poolSettings Dictionary를 찾을 필요 없이, 인자로 받은 poolData를 바로 사용)
        if (poolData.currentReserveCount < poolData.maxReserveSize)
        {
            poolData.currentReserveCount++;
            Debug.LogWarning($"[동적 풀 확장] {poolData.name} 추가 생성 (현재 추가량: {poolData.currentReserveCount}/{poolData.maxReserveSize})");

            // poolParents 딕셔너리에서 생성할 부모 트랜스폼을 찾습니다.
            Transform parentTransform = poolParents.ContainsKey(poolData) ? poolParents[poolData] : this.transform;
            return InstantiateEnemy(poolData, parentTransform);
        }

        return null;
    }

    private void SpawnEnemyAtRandomCircle()
    {
        WaveEnemyData clusterTheme = GetRandomEnemyDataFromWave();
        if (clusterTheme == null || clusterTheme.targetPool == null) return;

        bool isPurePack = clusterTheme.spawnAsPurePack;
        Vector2 centerCircle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 clusterCenterXZ = new Vector3(
            playerTransform.position.x + centerCircle.x,
            0f,
            playerTransform.position.z + centerCircle.y
        );

        int spawnCount = Random.Range(currentWave.minSpawnCount, currentWave.maxSpawnCount + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            WaveEnemyData targetData = isPurePack ? clusterTheme : GetRandomEnemyDataFromWave();
            if (targetData == null || targetData.targetPool == null) continue;

            // [변경됨] SO 안에 있는 targetPool 자체를 넘겨줍니다.
            Enemy enemyToSpawn = GetEnemyFromPool(targetData.targetPool);
            if (enemyToSpawn == null) continue;

            Vector2 randomOffset = Random.insideUnitCircle * clusterRadius;
            Vector3 rayStartPos = new Vector3(
                clusterCenterXZ.x + randomOffset.x,
                playerTransform.position.y + raycastStartHeight,
                clusterCenterXZ.z + randomOffset.y
            );

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

        // [변경됨] 몹이 기억하고 있는 originPool을 Key로 사용하여 풀에 반환합니다.
        if (enemy.originPool != null && enemyPools.ContainsKey(enemy.originPool))
        {
            enemyPools[enemy.originPool].Enqueue(enemy);
        }
        else
        {
            Destroy(enemy.gameObject); // 비정상적인 경우 안전장치
        }
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

    public Vector3 GetSeparationForce(Enemy targetEnemy, float radius)
    {
        Vector3 force = Vector3.zero;
        Vector2Int cell = GetCellPos(targetEnemy.transform.position);
        float sqrRadius = radius * radius;
        int checkCount = 0;
        const int MAX_CHECKS = 5;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int checkCell = new Vector2Int(cell.x + x, cell.y + y);

                if (spatialGrid.TryGetValue(checkCell, out List<Enemy> neighbors))
                {
                    for (int i = 0; i < neighbors.Count; i++)
                    {
                        if (checkCount >= MAX_CHECKS) return force;

                        Enemy neighbor = neighbors[i];
                        if (neighbor == targetEnemy) continue;

                        Vector3 diff = targetEnemy.transform.position - neighbor.transform.position;
                        diff.y = 0;
                        float sqrDist = diff.sqrMagnitude;

                        if (sqrDist > 0 && sqrDist < sqrRadius)
                        {
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