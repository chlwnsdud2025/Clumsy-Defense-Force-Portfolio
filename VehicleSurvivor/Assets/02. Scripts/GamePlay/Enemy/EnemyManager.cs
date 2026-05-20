using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("Current Stage Setting")]
    [Tooltip("현재 맵에 해당하는 테마 SO를 할당하세요.")]
    public StageThemeData currentTheme;

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

    [Header("VFX Management")]
    private Transform fxParent;
    public Transform FxParent => fxParent;

    // 내부 타이머 및 상태
    private float spawnTimer = 0f;
    private float gameTimer = 0f;
    private float gridUpdateTimer = 0f;

    // 데이터 관리 컬렉션
    private Dictionary<Vector2Int, List<Enemy>> spatialGrid = new Dictionary<Vector2Int, List<Enemy>>();
    private Dictionary<EnemyPoolDataSO, Queue<Enemy>> enemyPools = new Dictionary<EnemyPoolDataSO, Queue<Enemy>>();
    private Dictionary<EnemyPoolDataSO, Transform> poolParents = new Dictionary<EnemyPoolDataSO, Transform>();
    private List<Enemy> activeEnemies = new List<Enemy>();

    private List<WaveDataSO> sortedWaves;
    private WaveDataSO currentWave;
    private int currentWaveIndex = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 이펙트들을 담을 부모 빈 오브젝트 생성
        GameObject fxParentObj = new GameObject("Runtime_VFX_Container");
        fxParent = fxParentObj.transform;
    }

    void Start()
    {
        if (currentTheme != null)
        {
            ApplyTheme(currentTheme);
        }
    }

    public void ApplyTheme(StageThemeData theme)
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
            GameObject parentObj = new GameObject($"{poolData.name}_Pool");
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

            // [추가됨] 0초(게임 시작) 웨이브에도 보스가 있다면 즉시 소환
            if (currentWave.midBossPrefabs != null && currentWave.midBossPrefabs.Count > 0)
            {
                SpawnBossDirectly(currentWave);
            }
        }

        Debug.Log($"[Theme Applied] {theme.themeName} 테마 로딩 완료");
    }

    private Enemy InstantiateEnemy(EnemyPoolDataSO data, Transform parent)
    {
        GameObject obj = Instantiate(data.prefab, parent);
        obj.SetActive(false);
        Enemy enemy = obj.GetComponent<Enemy>();

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

               
                //새로운 웨이브 시작 시 보스 소환 체크
                
                if (currentWave.midBossPrefabs != null && currentWave.midBossPrefabs.Count > 0)
                {
                    SpawnBossDirectly(currentWave);
                }
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

    // ==========================================
    // [추가됨] 보스 직접 소환 로직
    // ==========================================
    private void SpawnBossDirectly(WaveDataSO wave)
    {
        // 1. 후보 리스트 중 하나를 무작위로 선택
        GameObject bossPrefab = wave.midBossPrefabs[Random.Range(0, wave.midBossPrefabs.Count)];
        if (bossPrefab == null) return;

        for (int i = 0; i < wave.midBossCount; i++)
        {
            // 2. 플레이어 주변 원형 바운더리 안에서 랜덤 위치 계산
            Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
            Vector3 spawnPosXZ = new Vector3(
                playerTransform.position.x + randomCircle.x,
                0f,
                playerTransform.position.z + randomCircle.y
            );

            // 3. 레이캐스트를 이용해 해당 위치의 정확한 지형 높이(y)를 찾음
            Vector3 rayStart = spawnPosXZ + Vector3.up * raycastStartHeight;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
            {
                // 오브젝트 풀링을 사용하지 않고 즉시 Instantiate 처리
                GameObject bossObj = Instantiate(bossPrefab, hit.point, Quaternion.identity);

                // 생성된 보스의 타겟을 플레이어로 즉시 할당
                BossEnemy bossScript = bossObj.GetComponent<BossEnemy>();
                if (bossScript != null)
                {
                    bossScript.playerTarget = this.playerTransform;
                }

                Debug.Log($"[Boss Spawned] 중간 보스 '{bossPrefab.name}' 출현!");
            }
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

    private Enemy GetEnemyFromPool(EnemyPoolDataSO poolData)
    {
        if (poolData == null) return null;

        if (enemyPools.ContainsKey(poolData) && enemyPools[poolData].Count > 0)
        {
            return enemyPools[poolData].Dequeue();
        }

        if (poolData.currentReserveCount < poolData.maxReserveSize)
        {
            poolData.currentReserveCount++;
            Debug.LogWarning($"[동적 풀 확장] {poolData.name} 추가 생성 (현재 추가량: {poolData.currentReserveCount}/{poolData.maxReserveSize})");

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

        if (enemy.originPool != null && enemyPools.ContainsKey(enemy.originPool))
        {
            enemyPools[enemy.originPool].Enqueue(enemy);
        }
        else
        {
            Destroy(enemy.gameObject);
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