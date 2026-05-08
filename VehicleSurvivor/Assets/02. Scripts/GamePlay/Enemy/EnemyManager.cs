using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyPoolSetting
{
    public EnemyType type;
    public GameObject prefab;
    public int poolSize = 50;

    // 유저님이 제안하신 스폰 비율(가중치) 변수! 기본값을 1로 두어 스폰이 아예 안 되는 것을 방지합니다.
    public float amountRate = 1f;
}

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("References")]
    public Transform playerTransform;

    [Header("Pool Settings")]
    public EnemyPoolSetting[] poolSettings;

    [Header("Spawn Settings")]
    public float spawnRadius = 50f;
    public float spawnInterval = 0.5f;
    private float spawnTimer = 0f;

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

    // ★ 유저님의 아이디어가 적용된 가중치 랜덤 스폰 로직!
    private void SpawnEnemyAtRandomCircle()
    {
        if (poolSettings.Length == 0) return;

        // 1. 모든 몹의 비율(amountRate)을 다 합친 총합을 구합니다.
        float totalRate = 0f;
        for (int i = 0; i < poolSettings.Length; i++)
        {
            totalRate += poolSettings[i].amountRate;
        }

        // 만약 모든 비율이 0이라면 아무것도 스폰하지 않습니다. (특정 웨이브 대기용으로 쓸 수 있음)
        if (totalRate <= 0f) return;

        // 2. 0부터 총합 사이의 랜덤한 숫자를 하나 뽑습니다.
        float randomValue = Random.Range(0f, totalRate);
        EnemyType typeToSpawn = poolSettings[0].type;

        // 3. 룰렛판을 돌리듯, 랜덤 숫자가 어느 몹의 비율 범위에 속하는지 찾습니다.
        float cumulativeRate = 0f;
        for (int i = 0; i < poolSettings.Length; i++)
        {
            cumulativeRate += poolSettings[i].amountRate;
            if (randomValue <= cumulativeRate)
            {
                typeToSpawn = poolSettings[i].type;
                break; // 당첨되었으니 반복문 즉시 탈출
            }
        }

        // 당첨된 타입의 풀에 남아있는 몹이 없다면 스폰 건너뜀 (풀 고갈 방어)
        if (enemyPools[typeToSpawn].Count == 0) return;

        // 스폰 위치 계산 및 생성
        Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 spawnPos = new Vector3(randomCircle.x, 0f, randomCircle.y) + playerTransform.position;

        Enemy enemy = enemyPools[typeToSpawn].Dequeue();
        enemy.SpawnSetup(playerTransform, spawnPos, this);
        activeEnemies.Add(enemy);
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