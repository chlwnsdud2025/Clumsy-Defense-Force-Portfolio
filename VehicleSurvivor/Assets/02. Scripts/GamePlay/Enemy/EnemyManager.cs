using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("References")]
    public GameObject enemyPrefab;
    public Transform playerTransform;

    [Header("Pool Settings")]
    public int maxEnemies = 200; // 최대 스폰 수

    [Header("Spawn Settings")]
    public float spawnRadius = 50f;     // 스폰 거리 
    public float spawnInterval = 0.5f;  // 스폰 간격
    private float spawnTimer = 0f;

    [Header("Grid & Separation Settings")]
    public float cellSize = 2f;
    private Dictionary<Vector2Int, List<Enemy>> spatialGrid = new Dictionary<Vector2Int, List<Enemy>>();


    private Queue<Enemy> enemyPool = new Queue<Enemy>(); // 비활성화 적
    private List<Enemy> activeEnemies = new List<Enemy>(); // 활성화 적

    void Start()
    {
        InitializePool();
    }

    private void InitializePool() // 오브젝트 풀링
    {
        for (int i = 0; i < maxEnemies; i++)
        {
            GameObject obj = Instantiate(enemyPrefab, transform);
            obj.SetActive(false);

            Enemy enemyScript = obj.GetComponent<Enemy>();
            enemyPool.Enqueue(enemyScript);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;

        UpdateGrid(); // 격자 지도 갱신


        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            activeEnemies[i].ManagedUpdate(dt);//개별 이동 로직 실행
        }

        spawnTimer += dt;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnEnemyAtRandomCircle();// 스폰
        }
    }

    // 플레이어 주변 원형 반경 랜덤 스폰
    private void SpawnEnemyAtRandomCircle()
    {
        //숫자 제한
        if (enemyPool.Count == 0) return;

        // 수학 공식: 유니티의 2D 랜덤 원형 좌표를 가져와서 테두리(.normalized)로 만든 뒤 반경을 곱함
        Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;

        // 3D 공간의 XZ 좌표로 변환하고, 플레이어의 현재 위치를 더해줍니다.
        Vector3 spawnPos = new Vector3(randomCircle.x, 0f, randomCircle.y) + playerTransform.position;

        
        Enemy enemy = enemyPool.Dequeue();
        enemy.SpawnSetup(playerTransform, spawnPos, this);
        activeEnemies.Add(enemy);
    }

    public void ReturnToPool(Enemy enemy)
    {
        enemy.gameObject.SetActive(false); 
        activeEnemies.Remove(enemy);
        enemyPool.Enqueue(enemy); 
    }

    // 매 프레임마다 적들을 격자 칸에 배정하는 함수
    private void UpdateGrid()
    {
        // 이전 프레임의 데이터 지우기 (List 재사용으로 가비지 생성 방지)
        foreach (var list in spatialGrid.Values)
        {
            list.Clear();
        }

        // 모든 활성화된 적을 현재 위치에 맞는 격자에 넣기
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

    // 월드 좌표를 격자 좌표로 변환
    private Vector2Int GetCellPos(Vector3 position)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x / cellSize), Mathf.FloorToInt(position.z / cellSize));
    }

    // 개별 Enemy가 호출할 함수: "내 주변 친구들을 피해갈 방향 벡터를 구해줘!"
    public Vector3 GetSeparationForce(Enemy targetEnemy, float radius)
    {
        Vector3 force = Vector3.zero;
        Vector2Int cell = GetCellPos(targetEnemy.transform.position);
        float sqrRadius = radius * radius;

        // 내 격자를 포함해 주변 3x3칸(총 9칸)만 검사합니다. (200마리 전수조사 방지!)
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
                        if (neighbor == targetEnemy) continue; // 자기 자신은 제외

                        Vector3 diff = targetEnemy.transform.position - neighbor.transform.position;
                        diff.y = 0; // 평면 계산
                        float sqrDist = diff.sqrMagnitude;

                        // 반경 안에 들어왔다면
                        if (sqrDist > 0 && sqrDist < sqrRadius)
                        {
                            // 가까울수록 더 강하게 밀어내는 힘을 누적합니다.
                            force += diff.normalized / Mathf.Sqrt(sqrDist);
                        }
                    }
                }
            }
        }
        return force;
    }
}