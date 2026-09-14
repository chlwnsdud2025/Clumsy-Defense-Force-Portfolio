using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    public const float DefaultPlayerBlockRadius = 0.28f;
    public const float MaxEnemyBlockRadius = 1.5f;
    public const int SeparationMaxChecks = 5;

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

    public float PlayerBlockRadius { get; private set; } = DefaultPlayerBlockRadius;
    public Transform FxParent => fxParent;
    public IReadOnlyList<Enemy> ActiveEnemies => activeEnemies;
    public IReadOnlyDictionary<Vector2Int, List<Enemy>> SpatialGrid => spatialGrid;

    private Transform fxParent;
    private float spawnTimer = 0f;
    private float gridUpdateTimer = 0f;
    private WaveEnemy_DataSO lastWave = null;
    private int lastPhaseIndex = -1;
    private Dictionary<Vector2Int, List<Enemy>> spatialGrid = new Dictionary<Vector2Int, List<Enemy>>();
    private Dictionary<EnemyPoolDataSO, Queue<Enemy>> enemyPools = new Dictionary<EnemyPoolDataSO, Queue<Enemy>>();
    private Dictionary<EnemyPoolDataSO, Transform> poolParents = new Dictionary<EnemyPoolDataSO, Transform>();
    private List<Enemy> activeEnemies = new List<Enemy>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        GameObject fxParentObj = new GameObject("Runtime_VFX_Container");
        fxParent = fxParentObj.transform;
        fxParent.SetParent(transform);
    }

    private void Start()
    {
        if (playerTransform != null)
            RefreshPlayerBlockRadius();
    }

    private void Update()
    {
        if (playerTransform == null || StageRunManager.Instance == null)
            return;

        if (!RunManager.TryGetInstance(out RunManager runManager) ||
            runManager.RunSession == null ||
            !runManager.RunSession.ShouldUpdateGameplay)
            return;

        if (StageRunManager.Instance.currentPhaseIndex != lastPhaseIndex)
        {
            lastPhaseIndex = StageRunManager.Instance.currentPhaseIndex;
            lastWave = null;
        }

        WaveEnemy_DataSO currentWave = StageRunManager.Instance.GetCurrentWave();
        if (currentWave == null) return;

        if (currentWave != lastWave)
        {
            lastWave = currentWave;
            OnWaveStarted(currentWave);
        }

        float dt = Time.deltaTime;

        gridUpdateTimer += dt;
        if (gridUpdateTimer >= gridUpdateInterval)
        {
            gridUpdateTimer = 0f;
            UpdateGrid();
        }

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
            activeEnemies[i].ManagedUpdate(dt);

        spawnTimer += dt;
        if (spawnTimer >= currentWave.spawnInterval)
        {
            spawnTimer = 0f;
            SpawnEnemyAtRandomCircle(currentWave);
        }
    }

    public void SetPlayerTransform(Transform transform)
    {
        playerTransform = transform;
        RefreshPlayerBlockRadius();
    }

    public void ApplyTheme(StageTheme_SO theme)
    {
        ClearAllPools();

        foreach (EnemyPoolDataSO poolData in theme.enemyPools)
        {
            if (poolData == null) continue;
            poolData.currentReserveCount = 0;

            Queue<Enemy> pool = new Queue<Enemy>();
            GameObject parentObj = new GameObject($"{poolData.name}_Pool");
            parentObj.transform.SetParent(transform);
            poolParents.Add(poolData, parentObj.transform);

            for (int i = 0; i < poolData.poolSize; i++)
            {
                Enemy enemy = InstantiateEnemy(poolData, parentObj.transform);
                pool.Enqueue(enemy);
            }

            enemyPools.Add(poolData, pool);
        }

        lastWave = null;
        lastPhaseIndex = -1;

        Debug.Log($"[EnemyManager] {theme.themeName} 테마 풀링 완료");
    }

    public void ReturnToPool(Enemy enemy)
    {
        enemy.gameObject.SetActive(false);
        activeEnemies.Remove(enemy);
        if (enemy.originPool != null && enemyPools.ContainsKey(enemy.originPool))
            enemyPools[enemy.originPool].Enqueue(enemy);
        else
            Destroy(enemy.gameObject);
    }

    public void ClearAllActiveEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = activeEnemies[i];
            if (enemy != null)
                ReturnToPool(enemy);
        }

        Debug.Log("[EnemyManager] 필드의 모든 잡몹을 풀로 회수 완료했습니다!");
    }

    public Vector3 GetSeparationForce(Enemy targetEnemy, float radius)
    {
        Vector3 force = Vector3.zero;
        Vector2Int cell = GetCellPos(targetEnemy.transform.position);
        float sqrRadius = radius * radius;
        int checkCount = 0;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int checkCell = new Vector2Int(cell.x + x, cell.y + y);
                if (!spatialGrid.TryGetValue(checkCell, out List<Enemy> neighbors))
                    continue;

                for (int i = 0; i < neighbors.Count; i++)
                {
                    if (checkCount >= SeparationMaxChecks) return force;

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

        return force;
    }

    public void GetNearbyEnemiesForBlock(Vector3 position, float radius, List<Enemy> buffer)
    {
        buffer.Clear();
        if (radius <= 0f)
            return;

        Vector2Int centerCell = GetCellPos(position);
        float sqrRadius = radius * radius;
        int cellRange = Mathf.CeilToInt(radius / cellSize);

        for (int x = -cellRange; x <= cellRange; x++)
        {
            for (int z = -cellRange; z <= cellRange; z++)
            {
                Vector2Int checkCell = new Vector2Int(centerCell.x + x, centerCell.y + z);
                if (!spatialGrid.TryGetValue(checkCell, out List<Enemy> neighbors))
                    continue;

                for (int i = 0; i < neighbors.Count; i++)
                {
                    Enemy enemy = neighbors[i];
                    if (enemy == null)
                        continue;

                    Vector3 diff = enemy.transform.position - position;
                    diff.y = 0f;
                    if (diff.sqrMagnitude <= sqrRadius)
                        buffer.Add(enemy);
                }
            }
        }
    }

    private void RefreshPlayerBlockRadius()
    {
        if (playerTransform == null)
        {
            PlayerBlockRadius = DefaultPlayerBlockRadius;
            return;
        }

        PlayerMotor playerMotor = playerTransform.GetComponentInParent<PlayerMotor>();
        PlayerBlockRadius = playerMotor != null ? playerMotor.blockRadius : DefaultPlayerBlockRadius;
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
        foreach (Transform parent in poolParents.Values)
            Destroy(parent.gameObject);

        enemyPools.Clear();
        poolParents.Clear();
        activeEnemies.Clear();
        spatialGrid.Clear();
    }

    private void OnWaveStarted(WaveEnemy_DataSO newWave)
    {
        Debug.Log($"[Wave Start] 새로운 웨이브('{newWave.name}') 시작!");

        if (newWave.miniBossPrefabs != null && newWave.miniBossPrefabs.Count > 0)
            SpawnMiniBossDirectly(newWave);
    }

    private void SpawnMiniBossDirectly(WaveEnemy_DataSO wave)
    {
        GameObject bossPrefab = wave.miniBossPrefabs[Random.Range(0, wave.miniBossPrefabs.Count)];
        if (bossPrefab == null) return;

        for (int i = 0; i < wave.miniBossCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
            Vector3 spawnPosXZ = new Vector3(
                playerTransform.position.x + randomCircle.x,
                0f,
                playerTransform.position.z + randomCircle.y
            );

            Vector3 rayStart = spawnPosXZ + Vector3.up * raycastStartHeight;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
            {
                GameObject bossObj = Instantiate(bossPrefab, hit.point, Quaternion.identity);

                BossEnemy bossScript = bossObj.GetComponent<BossEnemy>();
                if (bossScript != null) bossScript.playerTarget = playerTransform;

                Debug.Log($"[Mini-Boss Spawn] 웨이브 미니 보스 '{bossPrefab.name}' 출현!");
            }
        }
    }

    private WaveEnemy_Data GetRandomEnemyDataFromWave(WaveEnemy_DataSO currentWave)
    {
        if (currentWave.activeEnemies.Count == 0) return null;

        float totalWeight = 0f;
        for (int i = 0; i < currentWave.activeEnemies.Count; i++)
            totalWeight += currentWave.activeEnemies[i].spawnWeight;

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
            return enemyPools[poolData].Dequeue();

        if (poolData.currentReserveCount < poolData.maxReserveSize)
        {
            poolData.currentReserveCount++;
            Transform parentTransform = poolParents.ContainsKey(poolData) ? poolParents[poolData] : transform;
            return InstantiateEnemy(poolData, parentTransform);
        }

        return null;
    }

    private void SpawnEnemyAtRandomCircle(WaveEnemy_DataSO currentWave)
    {
        WaveEnemy_Data clusterTheme = GetRandomEnemyDataFromWave(currentWave);
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
            WaveEnemy_Data targetData = isPurePack ? clusterTheme : GetRandomEnemyDataFromWave(currentWave);
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

    private void UpdateGrid()
    {
        foreach (List<Enemy> list in spatialGrid.Values)
            list.Clear();

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            Enemy enemy = activeEnemies[i];
            Vector2Int cell = GetCellPos(enemy.transform.position);
            if (!spatialGrid.ContainsKey(cell))
                spatialGrid[cell] = new List<Enemy>(10);

            spatialGrid[cell].Add(enemy);
        }
    }

    public Vector2Int GetCellPos(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.z / cellSize));
    }
}
