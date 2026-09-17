using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    public const float DefaultPlayerBlockRadius = 0.28f;
    public const float MaxEnemyBlockRadius = 1.5f;
    public const int SeparationMaxChecks = 5;
    public const int SeparationCandidateBudget = 32;
    private const int MaxCachedCellLists = 64;
    private const float MinSeparationDistance = 0.25f;

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
    [Tooltip("외부 Transform 변경을 보정하는 점검 주기. 일반 이동/스폰/반환은 즉시 셀에 반영합니다.")]
    public float gridUpdateInterval = 0.15f;

    [Header("Recycle Rules")]
    [Tooltip("플레이어와 XZ 거리가 이 값(m)을 넘은 적은 플레이어 진행 방향 앞쪽 스폰 링으로 재배치합니다. 시야가 넓은 게임이라 기본값을 크게 둡니다.")]
    public float recycleDistance = 300f;
    [Tooltip("이 높이(y) 아래로 떨어진 적은 맵 밖 추락으로 보고 재배치합니다.")]
    public float killY = -50f;
    [Tooltip("이 시간(초) 동안 거의 움직이지 않은 적은 끼임으로 보고 재배치합니다.")]
    public float stuckSeconds = 6f;
    [Tooltip("체크 간격 동안 이동량이 이 거리(m) 미만이면 정지로 간주합니다.")]
    public float stuckMoveThreshold = 0.15f;
    [Tooltip("패턴 사거리 안에서 멈춘 적을 끼임으로 오판하지 않도록, patternRange에 더하는 여유 거리(m)")]
    public float stuckRangeMargin = 2f;
    [Tooltip("회수 규칙 검사 주기(초)")]
    public float recycleCheckInterval = 0.5f;
    [Tooltip("켜면 재배치, 끄면 풀로 회수만 합니다.")]
    public bool relocateOnRecycle = true;

    [Header("Benchmark Toggles (측정용 · 플레이 전 설정 권장)")]
    [Tooltip("끄면 각 Enemy에 EnemySelfUpdater가 붙어 Unity가 개별 Update()로 갱신합니다 (적용 전 방식).")]
    public bool useManagedUpdate = true;
    [Tooltip("끄면 스폰마다 Instantiate, 사망 시 Destroy 합니다 (적용 전 방식). 종류별 활성 수는 poolSize를 상한으로 두어 풀링 ON과 같은 적 수를 유지합니다.")]
    public bool usePooling = true;
    [Tooltip("끄면 분리 힘·차단 계산이 Grid 대신 전체 Enemy를 순회합니다 (적용 전 방식).")]
    public bool useSpatialGrid = true;

    /// <summary>직전 프레임의 이웃 거리 검사 횟수 (Grid on/off 비교용)</summary>
    public int LastFrameDistanceChecks { get; private set; }
    /// <summary>이번 프레임 게임플레이 갱신이 진행 중인지 (EnemySelfUpdater가 참조)</summary>
    public bool GameplayActive { get; private set; }
    /// <summary>측정용 누적 카운터: 이번 런에서 Instantiate / Destroy 한 Enemy 수</summary>
    public int InstantiateCount { get; private set; }
    public int DestroyCount { get; private set; }

    private static readonly ProfilerMarker MarkerUpdateLoop = new ProfilerMarker("EnemyManager.ManagedUpdateLoop");
    private static readonly ProfilerMarker MarkerUpdateGrid = new ProfilerMarker("EnemyManager.UpdateGrid");
    private static readonly ProfilerMarker MarkerSeparation = new ProfilerMarker("EnemyManager.Separation");
    private static readonly ProfilerMarker MarkerBlockQuery = new ProfilerMarker("EnemyManager.BlockQuery");
    private static readonly ProfilerMarker MarkerSpawn = new ProfilerMarker("EnemyManager.Spawn");
    private static readonly ProfilerMarker MarkerRecycle = new ProfilerMarker("EnemyManager.Recycle");
    private static readonly ProfilerMarker MarkerApplyTheme = new ProfilerMarker("EnemyManager.ApplyTheme");

    private int distanceChecks;
    private bool appliedManagedUpdate = true;
    private Dictionary<EnemyPoolDataSO, int> activeCountByPool = new Dictionary<EnemyPoolDataSO, int>();

    public float PlayerBlockRadius { get; private set; } = DefaultPlayerBlockRadius;
    public Transform FxParent => fxParent;
    public IReadOnlyList<Enemy> ActiveEnemies => activeEnemies;
    public IReadOnlyDictionary<Vector2Int, List<Enemy>> SpatialGrid => spatialGrid;

    private Transform fxParent;
    private float spawnTimer = 0f;
    private float gridUpdateTimer = 0f;
    private float recycleTimer = 0f;
    private WaveEnemy_DataSO lastWave = null;
    private int lastPhaseIndex = -1;
    private Dictionary<Vector2Int, List<Enemy>> spatialGrid = new Dictionary<Vector2Int, List<Enemy>>();
    private Dictionary<Enemy, Vector2Int> enemyGridCells = new Dictionary<Enemy, Vector2Int>();
    private readonly HashSet<Enemy> trackedEnemies = new HashSet<Enemy>();
    private readonly Stack<List<Enemy>> freeCellLists = new Stack<List<Enemy>>();
    private readonly List<Enemy> updateSnapshot = new List<Enemy>();
    private readonly List<List<Enemy>> queryCells = new List<List<Enemy>>(9);
    private readonly Enemy[] nearestEnemies = new Enemy[SeparationMaxChecks];
    private readonly float[] nearestDistances = new float[SeparationMaxChecks];
    private float indexedCellSize = 2f;
    private bool indexedGridEnabled = true;
    private Dictionary<EnemyPoolDataSO, Queue<Enemy>> enemyPools = new Dictionary<EnemyPoolDataSO, Queue<Enemy>>();
    private Dictionary<EnemyPoolDataSO, Transform> poolParents = new Dictionary<EnemyPoolDataSO, Transform>();
    private List<Enemy> activeEnemies = new List<Enemy>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

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
        if (useManagedUpdate != appliedManagedUpdate)
            ApplyManagedUpdateMode();

        LastFrameDistanceChecks = distanceChecks;
        distanceChecks = 0;
        GameplayActive = false;

        if (playerTransform == null)
            return;

        if (!RunManager.TryGetInstance(out RunManager runManager) ||
            runManager.RunSession == null ||
            !runManager.RunSession.ShouldUpdateGameplay)
            return;

        StageRunManager stage = StageRunManager.Instance;
        if (stage != null && stage.currentPhaseIndex != lastPhaseIndex)
        {
            lastPhaseIndex = stage.currentPhaseIndex;
            lastWave = null;
            spawnTimer = 0f;
        }

        WaveEnemy_DataSO currentWave = stage != null ? stage.GetCurrentWave() : null;

        GameplayActive = true;

        if (currentWave != lastWave)
        {
            lastWave = currentWave;
            if (currentWave != null) OnWaveStarted(currentWave);
        }

        float dt = Time.deltaTime;

        EnsureGridConfiguration();
        gridUpdateTimer += dt;
        if (useSpatialGrid && gridUpdateTimer >= gridUpdateInterval)
        {
            gridUpdateTimer = 0f;
            using (MarkerUpdateGrid.Auto())
                UpdateGrid();
        }

        if (useManagedUpdate)
        {
            using (MarkerUpdateLoop.Auto())
            {
                // 갱신 중 사망/외부 소환으로 목록이 바뀌어도 같은 개체를 중복 갱신하지 않는다.
                updateSnapshot.Clear();
                updateSnapshot.AddRange(activeEnemies);
                for (int i = updateSnapshot.Count - 1; i >= 0; i--)
                {
                    Enemy enemy = updateSnapshot[i];
                    if (enemy != null && enemy.isActiveAndEnabled && trackedEnemies.Contains(enemy))
                        enemy.ManagedUpdate(dt);
                }
            }
        }

        recycleTimer += dt;
        if (recycleTimer >= recycleCheckInterval)
        {
            float elapsed = recycleTimer;
            recycleTimer = 0f;
            using (MarkerRecycle.Auto())
                CheckRecycle(elapsed);
        }

        if (currentWave == null) { spawnTimer = 0f; return; }
        spawnTimer += dt;
        if (spawnTimer >= currentWave.spawnInterval)
        {
            spawnTimer = 0f;
            using (MarkerSpawn.Auto())
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
        using ProfilerMarker.AutoScope scope = MarkerApplyTheme.Auto();
        ClearAllPools();

        foreach (EnemyPoolDataSO poolData in theme.CollectEnemyPools())
        {
            if (poolData == null) continue;
            poolData.currentReserveCount = 0;
            activeCountByPool[poolData] = 0;

            Queue<Enemy> pool = new Queue<Enemy>();
            GameObject parentObj = new GameObject($"{poolData.name}_Pool");
            parentObj.transform.SetParent(transform);
            poolParents.Add(poolData, parentObj.transform);

            // 풀링 OFF(측정용)면 미리 만들지 않고 스폰 시점에 Instantiate 한다
            if (usePooling)
            {
                for (int i = 0; i < poolData.poolSize; i++)
                {
                    Enemy enemy = InstantiateEnemy(poolData, parentObj.transform);
                    pool.Enqueue(enemy);
                }
            }

            enemyPools.Add(poolData, pool);
        }

        lastWave = null;
        lastPhaseIndex = -1;

        Debug.Log($"[EnemyManager] {theme.themeName} 테마 풀링 완료");
    }

    public void ReturnToPool(Enemy enemy)
    {
        if (enemy == null) return;
        RemoveFromGrid(enemy);
        UnregisterEnemy(enemy);
        enemy.gameObject.SetActive(false);

        // 풀링 OFF(측정용): 적용 전 방식대로 파괴
        if (!usePooling)
        {
            Destroy(enemy.gameObject);
            DestroyCount++;
            return;
        }

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
        using ProfilerMarker.AutoScope scope = MarkerSeparation.Auto();
        int count = SelectSeparationNeighbors(targetEnemy, radius);
        Vector3 force = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            Enemy neighbor = nearestEnemies[i];
            Vector3 diff = targetEnemy.transform.position - neighbor.transform.position;
            diff.y = 0f;
            float distance = Mathf.Sqrt(nearestDistances[i]);
            Vector3 direction = distance > 0.0001f ? diff / distance : CoincidentDirection(targetEnemy, neighbor);
            force += direction / Mathf.Max(distance, MinSeparationDistance);
        }
        // 아주 가까운 개체 때문에 속도가 발산하거나 셀을 순간적으로 건너뛰지 않게 한다.
        return Vector3.ClampMagnitude(force, SeparationMaxChecks);
    }

    // 예산 안에서 조사한 후보 중 가까운 5개. 밀집 시 전체 이웃의 정확한 최근접 5개는 보장하지 않는다.
    private int SelectSeparationNeighbors(Enemy targetEnemy, float radius, List<Enemy> inspected = null, bool recordChecks = true)
    {
        if (targetEnemy == null || radius <= 0f) return 0;
        queryCells.Clear();
        int count = 0;
        float sqrRadius = radius * radius;
        if (!useSpatialGrid)
        {
            // 측정용 전체 목록 기준선: 전수 검색 후 최근접 5개.
            foreach (Enemy enemy in activeEnemies)
                ConsiderSeparationNeighbor(targetEnemy, enemy, sqrRadius, ref count, inspected, recordChecks);
            return count;
        }

        Vector2Int center = GetCellPos(targetEnemy.transform.position);
        int range = Mathf.CeilToInt(radius / cellSize);
        int largestCell = 0;
        for (int x = -range; x <= range; x++)
        for (int z = -range; z <= range; z++)
        {
            if (!spatialGrid.TryGetValue(new Vector2Int(center.x + x, center.y + z), out List<Enemy> cell)) continue;
            queryCells.Add(cell);
            largestCell = Mathf.Max(largestCell, cell.Count);
        }

        // 셀을 번갈아 조사하고 시작 인덱스를 회전시켜 특정 셀/개체만 계속 선택되지 않게 한다.
        uint seed = unchecked((uint)targetEnemy.GetInstanceID() + (uint)Time.frameCount * 17u);
        int visited = 0;
        for (int round = 0; round < largestCell && visited < SeparationCandidateBudget; round++)
        {
            for (int c = 0; c < queryCells.Count && visited < SeparationCandidateBudget; c++)
            {
                List<Enemy> cell = queryCells[c];
                if (round >= cell.Count) continue;
                int index = (int)((seed + (uint)round) % (uint)cell.Count);
                visited++;
                ConsiderSeparationNeighbor(targetEnemy, cell[index], sqrRadius, ref count, inspected, recordChecks);
            }
        }
        return count;
    }

    private void ConsiderSeparationNeighbor(Enemy target, Enemy candidate, float sqrRadius,
        ref int count, List<Enemy> inspected, bool recordChecks)
    {
        if (candidate == null || candidate == target || !candidate.isActiveAndEnabled) return;
        if (recordChecks) distanceChecks++;
        inspected?.Add(candidate);
        Vector3 diff = target.transform.position - candidate.transform.position;
        diff.y = 0f;
        float sqr = diff.sqrMagnitude;
        if (sqr >= sqrRadius) return;
        int index = count;
        while (index > 0 && (sqr < nearestDistances[index - 1] ||
            (sqr == nearestDistances[index - 1] && candidate.GetInstanceID() < nearestEnemies[index - 1].GetInstanceID())))
            index--;
        if (index >= SeparationMaxChecks) return;
        int last = Mathf.Min(count, SeparationMaxChecks - 1);
        for (int i = last; i > index; i--)
        {
            nearestEnemies[i] = nearestEnemies[i - 1];
            nearestDistances[i] = nearestDistances[i - 1];
        }
        nearestEnemies[index] = candidate;
        nearestDistances[index] = sqr;
        count = Mathf.Min(count + 1, SeparationMaxChecks);
    }

    private static Vector3 CoincidentDirection(Enemy a, Enemy b)
    {
        int first = a.GetInstanceID(), second = b.GetInstanceID();
        uint low = unchecked((uint)Mathf.Min(first, second));
        uint high = unchecked((uint)Mathf.Max(first, second));
        uint hash = unchecked(low * 73856093u ^ high * 19349663u);
        float angle = (hash % 1024u) * (Mathf.PI * 2f / 1024f);
        Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        return first < second ? direction : -direction;
    }

    // Scene 뷰도 실제 선택기를 사용하되 성능 측정 카운터에는 포함하지 않는다.
    public void GetSeparationDebugNeighbors(Enemy target, List<Enemy> selected, List<Enemy> inspected)
    {
        selected.Clear();
        inspected.Clear();
        int count = SelectSeparationNeighbors(target, target.separationRadius, inspected, false);
        for (int i = 0; i < count; i++) selected.Add(nearestEnemies[i]);
    }

    public void GetNearbyEnemiesForBlock(Vector3 position, float radius, List<Enemy> buffer)
    {
        using ProfilerMarker.AutoScope scope = MarkerBlockQuery.Auto();

        buffer.Clear();
        if (radius <= 0f)
            return;

        // Grid OFF(측정용): 전체 Enemy를 순회
        if (!useSpatialGrid)
        {
            float sqrR = radius * radius;
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                Enemy enemy = activeEnemies[i];
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                Vector3 d = enemy.transform.position - position;
                d.y = 0f;
                distanceChecks++;
                if (d.sqrMagnitude <= sqrR)
                    buffer.Add(enemy);
            }
            return;
        }

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
                    if (enemy == null || !enemy.isActiveAndEnabled)
                        continue;

                    Vector3 diff = enemy.transform.position - position;
                    diff.y = 0f;
                    distanceChecks++;
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
        InstantiateCount++;
        Enemy enemy = obj.GetComponent<Enemy>();
        enemy.originPool = data;
        if (!useManagedUpdate) obj.AddComponent<EnemySelfUpdater>();
        return enemy;
    }

    private void ClearAllPools()
    {
        foreach (Transform parent in poolParents.Values)
            Destroy(parent.gameObject);

        enemyPools.Clear();
        poolParents.Clear();
        activeEnemies.Clear();
        trackedEnemies.Clear();
        updateSnapshot.Clear();
        activeCountByPool.Clear();
        spatialGrid.Clear();
        enemyGridCells.Clear();
        freeCellLists.Clear();
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

        // 풀링 OFF(측정용): 매번 Instantiate. 종류별 활성 수는 poolSize를 상한으로 두어 풀링 ON과 같은 조건을 만든다
        if (!usePooling)
        {
            activeCountByPool.TryGetValue(poolData, out int active);
            if (active >= poolData.poolSize + poolData.maxReserveSize) return null;
            Transform parent = poolParents.TryGetValue(poolData, out Transform p) ? p : transform;
            return InstantiateEnemy(poolData, parent);
        }
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
                RegisterEnemy(enemyToSpawn);
            }
            else
            {
                ReturnToPool(enemyToSpawn);
            }
        }
    }

    // ManagedUpdate 토글 반영: OFF면 모든 Enemy(풀 대기 포함)에 EnemySelfUpdater를 붙이고, ON이면 떼어낸다
    private void ApplyManagedUpdateMode()
    {
        appliedManagedUpdate = useManagedUpdate;
        Enemy[] all = GetComponentsInChildren<Enemy>(true);
        for (int i = 0; i < all.Length; i++)
        {
            EnemySelfUpdater updater = all[i].GetComponent<EnemySelfUpdater>();
            if (useManagedUpdate && updater != null) Destroy(updater);
            else if (!useManagedUpdate && updater == null) all[i].gameObject.AddComponent<EnemySelfUpdater>();
        }
        // 보스가 자신의 컨테이너 아래에 소환한 Enemy도 동일하게 전환한다.
        foreach (Enemy enemy in activeEnemies)
            ConfigureEnemyUpdater(enemy);
        Debug.Log($"[EnemyManager] ManagedUpdate {(useManagedUpdate ? "ON" : "OFF")} 적용 ({all.Length}개)");
    }

    // ==========================================================
    // 회수 규칙: 추락 · 너무 멂 · 끼임 → 보상 없이 재배치(또는 회수)
    // ==========================================================
    private enum ERecycleReason { None, Fell, TooFar, Stuck }

    private void CheckRecycle(float elapsed)
    {
        Vector3 playerPos = playerTransform.position;
        float sqrRecycle = recycleDistance * recycleDistance;
        float sqrStuckMove = stuckMoveThreshold * stuckMoveThreshold;

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = activeEnemies[i];
            if (enemy == null) continue;

            Vector3 pos = enemy.transform.position;
            Vector3 toPlayerXZ = playerPos - pos;
            toPlayerXZ.y = 0f;
            float sqrDist = toPlayerXZ.sqrMagnitude;

            ERecycleReason reason = ERecycleReason.None;

            if (pos.y < killY)
                reason = ERecycleReason.Fell;
            else if (sqrDist > sqrRecycle)
                reason = ERecycleReason.TooFar;
            else
            {
                // 끼임: 사거리 밖(= 움직여야 하는 상황)인데 거의 이동하지 않음
                float stopRange = enemy.patternRange + stuckRangeMargin;
                bool shouldBeMoving = sqrDist > stopRange * stopRange;
                float moved = (pos - enemy.recycleLastPos).sqrMagnitude;

                if (shouldBeMoving && moved < sqrStuckMove)
                {
                    enemy.recycleStuckTime += elapsed;
                    if (enemy.recycleStuckTime >= stuckSeconds)
                        reason = ERecycleReason.Stuck;
                }
                else enemy.recycleStuckTime = 0f;
            }

            enemy.recycleLastPos = pos;

            if (reason != ERecycleReason.None)
                Recycle(enemy, reason);
        }
    }

    // Die()를 타지 않으므로 경험치·돈·킬 카운트가 발생하지 않는다.
    private void Recycle(Enemy enemy, ERecycleReason reason)
    {
        if (relocateOnRecycle && TryGetRelocatePoint(out Vector3 point))
        {
            enemy.SpawnSetup(playerTransform, point, this);
            return;
        }

        ReturnToPool(enemy);
    }

    // 플레이어 진행 방향 앞쪽(±60°)의 스폰 링 위 지면 지점을 찾는다.
    private bool TryGetRelocatePoint(out Vector3 point)
    {
        Vector3 forward = playerTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        for (int attempt = 0; attempt < 3; attempt++)
        {
            Vector3 dir = Quaternion.Euler(0f, Random.Range(-60f, 60f), 0f) * forward;
            Vector3 xz = playerTransform.position + dir * spawnRadius;
            Vector3 rayStart = new Vector3(xz.x, playerTransform.position.y + raycastStartHeight, xz.z);

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
            {
                point = hit.point;
                return true;
            }
        }

        point = Vector3.zero;
        return false;
    }

    private void RemoveFromGrid(Enemy enemy)
    {
        // 이동 후 현재 셀이 달라졌어도, 마지막으로 등록한 셀에서 제거한다.
        if (!enemyGridCells.TryGetValue(enemy, out Vector2Int cell))
            return;

        if (spatialGrid.TryGetValue(cell, out List<Enemy> neighbors))
        {
            neighbors.Remove(enemy);
            if (neighbors.Count == 0)
            {
                spatialGrid.Remove(cell);
                if (freeCellLists.Count < MaxCachedCellLists && neighbors.Capacity <= 128)
                    freeCellLists.Push(neighbors);
            }
        }

        enemyGridCells.Remove(enemy);
    }

    private void UpdateGrid()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = activeEnemies[i];
            if (enemy == null || !enemy.isActiveAndEnabled) UnregisterEnemy(enemy);
            else NotifyEnemyMoved(enemy);
        }
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null || !enemy.isActiveAndEnabled) return;
        if (trackedEnemies.Add(enemy))
        {
            activeEnemies.Add(enemy);
            if (enemy.originPool != null)
                activeCountByPool[enemy.originPool] = (activeCountByPool.TryGetValue(enemy.originPool, out int count) ? count : 0) + 1;
            ConfigureEnemyUpdater(enemy);
        }
        NotifyEnemyMoved(enemy);
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        // Unity에서 파괴된 참조도 컬렉션에서는 제거해야 한다.
        if (ReferenceEquals(enemy, null)) return;
        RemoveFromGrid(enemy);
        if (!trackedEnemies.Remove(enemy)) return;
        activeEnemies.Remove(enemy);
        if (enemy != null && enemy.originPool != null && activeCountByPool.ContainsKey(enemy.originPool))
            activeCountByPool[enemy.originPool]--;
    }

    public bool IsRegistered(Enemy enemy) => enemy != null && trackedEnemies.Contains(enemy);

    private void ConfigureEnemyUpdater(Enemy enemy)
    {
        if (enemy == null) return;
        EnemySelfUpdater updater = enemy.GetComponent<EnemySelfUpdater>();
        if (useManagedUpdate && updater != null) updater.enabled = false;
        else if (!useManagedUpdate)
        {
            if (updater == null) updater = enemy.gameObject.AddComponent<EnemySelfUpdater>();
            updater.enabled = true;
        }
    }

    public void NotifyEnemyMoved(Enemy enemy)
    {
        if (enemy == null || !trackedEnemies.Contains(enemy)) return;
        if (!useSpatialGrid || !enemy.isActiveAndEnabled) { RemoveFromGrid(enemy); return; }
        Vector2Int cell = GetCellPos(enemy.transform.position);
        if (enemyGridCells.TryGetValue(enemy, out Vector2Int oldCell) && oldCell == cell) return;
        RemoveFromGrid(enemy);
        if (!spatialGrid.TryGetValue(cell, out List<Enemy> neighbors))
        {
            neighbors = freeCellLists.Count > 0 ? freeCellLists.Pop() : new List<Enemy>(10);
            spatialGrid.Add(cell, neighbors);
        }
        neighbors.Add(enemy);
        enemyGridCells[enemy] = cell;
    }

    private void EnsureGridConfiguration()
    {
        cellSize = Mathf.Max(0.1f, cellSize);
        if (indexedCellSize == cellSize && indexedGridEnabled == useSpatialGrid) return;
        indexedCellSize = cellSize;
        indexedGridEnabled = useSpatialGrid;
        spatialGrid.Clear();
        enemyGridCells.Clear();
        if (useSpatialGrid) UpdateGrid();
    }

    public Vector2Int GetCellPos(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.z / cellSize));
    }
}
