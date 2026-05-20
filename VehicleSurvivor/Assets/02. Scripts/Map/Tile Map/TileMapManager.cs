using UnityEngine;

public class TileMapManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("타일맵이 생성될 기준 위치와 방향을 결정하는 오브젝트")]
    public Transform tileMapSpawnPoint;

    [Header("Tile Settings")]
    public GameObject tilePrefab; // 생성할 타일 프리팹 (PuzzleTile 스크립트 포함)
    public int width = 8;         // 가로 타일 개수 (X축)
    public int height = 10;       // 세로 타일 개수 (Z축 - 전진 방향)
    public float tileDistance = 2.5f; // 타일 간의 간격

    private GameObject[,] tileGrid;

    void Start()
    {
        // 만약 에디터에서 SpawnPoint를 깜빡하고 안 넣었다면, 이 스크립트가 달린 오브젝트를 기준으로 삼음 (안전장치)
        if (tileMapSpawnPoint == null)
        {
            tileMapSpawnPoint = this.transform;
        }

        GenerateTileMap();
        GenerateTruePath();
    }

    // 1. 전체 타일을 격자 형태로 생성
    void GenerateTileMap()
    {
        tileGrid = new GameObject[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                // [핵심 변경점] 단순 Vector3 덧셈이 아닌, 기준점의 로컬 축 방향(right, forward)을 기준으로 생성
                Vector3 spawnPos = tileMapSpawnPoint.position
                                 + (tileMapSpawnPoint.right * x * tileDistance)
                                 + (tileMapSpawnPoint.forward * z * tileDistance);

                // 생성 시 회전값(Rotation)도 기준점과 동일하게 맞춰줌
                GameObject newTile = Instantiate(tilePrefab, spawnPos, tileMapSpawnPoint.rotation, transform);

                newTile.name = $"Tile_{x}_{z}";
                tileGrid[x, z] = newTile;
            }
        }
    }

    // 2. 단 하나의 진짜 길 생성 알고리즘 (기존과 동일)
    void GenerateTruePath()
    {
        int currentX = Random.Range(0, width);

        for (int z = 0; z < height; z++)
        {
            SetTrueTile(currentX, z);

            if (z == height - 1) break;

            bool moveSideways = Random.value > 0.5f;

            if (moveSideways)
            {
                int targetX = Random.Range(0, width);
                int step = (targetX > currentX) ? 1 : -1;

                while (currentX != targetX)
                {
                    currentX += step;
                    SetTrueTile(currentX, z);
                }
            }
        }
    }

    // 특정 좌표의 타일을 진짜 길로 설정하는 함수 (기존과 동일)
    void SetTrueTile(int x, int z)
    {
        PuzzleTile tileLogic = tileGrid[x, z].GetComponent<PuzzleTile>();
        if (tileLogic != null)
        {
            tileLogic.isTruePath = true;

            // 테스트용: 게임 시작 시 진짜 길을 노란색으로 보고 싶다면 아래 주석 해제
            // tileGrid[x,z].GetComponent<Renderer>().material.color = Color.yellow;
        }
    }
}