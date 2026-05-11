using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyPoolSetting
{
    public EnemyType type;
    public GameObject prefab;
    public int poolSize = 50;

    // 스폰 비율(가중치) 변수
    public float amountRate = 1f;
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

    private Dictionary<EnemyType, Queue<Enemy>> enemyPools;
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

        foreach (var setting in poolSettings)
        {
            Queue<Enemy> pool = new Queue<Enemy>();
            GameObject poolParent = new GameObject($"{setting.type} Pool");
            poolParent.transform.SetParent(this.transform);

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

        UpdateGrid();

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

    private void SpawnEnemyAtRandomCircle()
    {
        if (poolSettings.Length == 0) return;

        // 1. 모든 몹의 비율(amountRate) 총합을 구합니다. (루프 밖에서 한 번만 연산)
        float totalRate = 0f;
        for (int i = 0; i < poolSettings.Length; i++)
        {
            totalRate += poolSettings[i].amountRate;
        }

        if (totalRate <= 0f) return;

        // 2. 군집 스폰 (Cluster Spawn) 중심점 및 마리 수 계산
        Vector2 centerCircle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 clusterCenterXZ = new Vector3(
            playerTransform.position.x + centerCircle.x,
            0f,
            playerTransform.position.z + centerCircle.y
        );

        int spawnCount = Random.Range(minSpawnCount, maxSpawnCount + 1);

        // 3. 결정된 마리 수만큼 반복하면서, '매번 개별적으로' 몹 타입을 뽑아 스폰합니다.
        for (int i = 0; i < spawnCount; i++)
        {
            // --- [수정된 부분] 반복문 안에서 매번 룰렛을 돌립니다 ---
            float randomValue = Random.Range(0f, totalRate);
            EnemyType typeToSpawn = poolSettings[0].type;
            float cumulativeRate = 0f;

            // 주의: 바깥쪽 반복문이 i를 쓰므로 안쪽은 j를 사용합니다.
            for (int j = 0; j < poolSettings.Length; j++)
            {
                cumulativeRate += poolSettings[j].amountRate;
                if (randomValue <= cumulativeRate)
                {
                    typeToSpawn = poolSettings[j].type;
                    break;
                }
            }

            // 풀 고갈 방어: 이 몹의 풀이 비었다면 전체를 취소(break)하지 않고 이 1마리만 건너뜀(continue)
            if (enemyPools[typeToSpawn].Count == 0) continue;
            // --------------------------------------------------------

            // 중심점을 기준으로 반경(clusterRadius) 내의 랜덤한 위치 오프셋 생성
            Vector2 randomOffset = Random.insideUnitCircle * clusterRadius;

            // 3D 지형을 인식하기 위한 상공 레이캐스트 시작점
            Vector3 rayStartPos = new Vector3(
                clusterCenterXZ.x + randomOffset.x,
                playerTransform.position.y + raycastStartHeight,
                clusterCenterXZ.z + randomOffset.y
            );

            // 하늘에서 아래로 레이캐스트(레이저) 발사
            if (Physics.Raycast(rayStartPos, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
            {
                // 바닥을 찾았다면 해당 위치(hit.point)의 정확한 높이에 몹을 스폰
                Enemy enemy = enemyPools[typeToSpawn].Dequeue();
                enemy.SpawnSetup(playerTransform, hit.point, this);
                activeEnemies.Add(enemy);
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
        foreach (var list in spatialGrid.Values)
        {
            list.Clear();
        }

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            Enemy enemy = activeEnemies[i];
            Vector2Int cell = GetCellPos(enemy.transform.position);

            if (!spatialGrid.ContainsKey(cell))
            {
                spatialGrid[cell] = new List<Enemy>(10);
            }
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

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int checkCell = new Vector2Int(cell.x + x, cell.y + y);

                if (spatialGrid.TryGetValue(checkCell, out List<Enemy> neighbors))
                {
                    for (int i = 0; i < neighbors.Count; i++)
                    {
                        Enemy neighbor = neighbors[i];
                        if (neighbor == targetEnemy) continue;

                        Vector3 diff = targetEnemy.transform.position - neighbor.transform.position;
                        diff.y = 0;
                        float sqrDist = diff.sqrMagnitude;

                        if (sqrDist > 0 && sqrDist < sqrRadius)
                        {
                            force += diff.normalized / Mathf.Sqrt(sqrDist);
                        }
                    }
                }
            }
        }
        return force;
    }
}