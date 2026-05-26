using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using TMPro;


public class MatchGameManager : MonoBehaviour
{
    [Header("타일 생성 설정 (TileMapManager 로직)")]
    public Transform tileMapSpawnPoint;
    public GameObject tilePrefab;
    public int width = 5;
    public int height = 5;
    public float tileDistance = 2.5f;

    [Header("색상 풀 (과일 종류)")]
    public Color[] availableColors = new Color[]
    {
        Color.red, Color.yellow, new Color(1f, 0.5f, 0f), Color.green
    };

    [Header("게임 룰 & 시간 설정")]
    public int requiredScore = 5; // 5번 생존 시 클리어
    public float memorizeTime = 3.0f;
    public float moveTime = 4.0f;
    public float dropTime = 3.0f;

    [Header("정답 표시판")]
    [Tooltip("정답 색상을 띄워줄 전광판 오브젝트의 Renderer를 연결하세요.")]
    public Renderer displayBoard;

    [Header("3D TextMesh UI 세팅")]
    [Tooltip("점수를 화면에 띄워줄 3D TextMesh 컴포넌트를 인스펙터에서 연결하세요.")]
    public TextMeshPro scoreTextMesh;

    [Header("생존 검사")]
    public BoxCollider safeZoneBox;
    public UnityEvent onStageClear;

    private List<MatchTile> _allTiles = new List<MatchTile>();
    private Color _currentAnswer;
    private int _playerScore = 0;
    private GameObject _player;

    // [추가] 게임이 완전히 클리어되었는지 저장하는 플래그
    private bool _isCleared = false;

    void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        if (tileMapSpawnPoint == null) tileMapSpawnPoint = this.transform;

        GenerateTileMap();
        UpdateScoreUI();

        StartCoroutine(GameLoopCoroutine());
    }

    private void GenerateTileMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 spawnPos = tileMapSpawnPoint.position
                                 + (tileMapSpawnPoint.right * x * tileDistance)
                                 + (tileMapSpawnPoint.forward * z * tileDistance);

                GameObject newTile = Instantiate(tilePrefab, spawnPos, tileMapSpawnPoint.rotation, transform);
                newTile.name = $"MatchTile_{x}_{z}";

                _allTiles.Add(newTile.GetComponent<MatchTile>());
            }
        }
    }

    private IEnumerator GameLoopCoroutine()
    {
        // [수정] 클리어되지 않은 동안에만 루프가 돌아갑니다.
        while (!_isCleared)
        {
            ResetAllTiles();
            AssignColorsWithMinimumThree();

            // 1. 색상 보여주기
            yield return new WaitForSeconds(memorizeTime);

            // 2. 색상 숨기고 정답 발표
            HideAllColors();
            ChooseAnswer();
            yield return new WaitForSeconds(moveTime);

            // 3. 오답 타일 추락
            DropWrongTiles();

            // 4. 추락 연출 대기
            yield return new WaitForSeconds(3f);

            // 5. 생존 검사 (이 내부에서 _isCleared가 true로 바뀔 수 있음)
            CheckPlayerSurvival();

            // [추가] 검사 직후 클리어되었다면 즉시 코루틴 루프를 탈출(종료)합니다!
            if (_isCleared)
            {
                break;
            }

            // 6. 다음 라운드까지 대기
            yield return new WaitForSeconds(dropTime - 1.5f);
        }

        // 루프 탈출 후 최종 정지 연출
        Debug.Log("MatchGameManager 시스템이 정상적으로 정지되었습니다.");
    }

    private void AssignColorsWithMinimumThree()
    {
        List<Color> colorPool = new List<Color>();
        int totalTiles = width * height;

        for (int i = 0; i < 3; i++)
        {
            foreach (Color c in availableColors)
            {
                colorPool.Add(c);
            }
        }

        int remaining = totalTiles - colorPool.Count;
        for (int i = 0; i < remaining; i++)
        {
            colorPool.Add(availableColors[Random.Range(0, availableColors.Length)]);
        }

        for (int i = 0; i < colorPool.Count; i++)
        {
            Color temp = colorPool[i];
            int randomIndex = Random.Range(i, colorPool.Count);
            colorPool[i] = colorPool[randomIndex];
            colorPool[randomIndex] = temp;
        }

        for (int i = 0; i < _allTiles.Count; i++)
        {
            _allTiles[i].SetColor(colorPool[i]);
        }
    }

    private void HideAllColors()
    {
        foreach (MatchTile tile in _allTiles) tile.HideColor();
    }

    private void ChooseAnswer()
    {
        _currentAnswer = _allTiles[Random.Range(0, _allTiles.Count)].myColor;
        if (displayBoard != null) displayBoard.material.color = _currentAnswer;
    }

    private void DropWrongTiles()
    {
        foreach (MatchTile tile in _allTiles)
        {
            if (tile.myColor != _currentAnswer) tile.Drop();
            else tile.ShowColor();
        }
    }

    private void ResetAllTiles()
    {
        foreach (MatchTile tile in _allTiles) tile.ResetTile();
        if (displayBoard != null) displayBoard.material.color = Color.black;
    }

    private void CheckPlayerSurvival()
    {
        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            if (_player == null) return;
        }

        if (safeZoneBox == null) return;

        bool isSurviving = false;
        Collider playerCollider = _player.GetComponent<Collider>();

        if (playerCollider != null)
        {
            isSurviving = safeZoneBox.bounds.Intersects(playerCollider.bounds);
        }
        else
        {
            isSurviving = safeZoneBox.bounds.Contains(_player.transform.position);
        }

        if (isSurviving)
        {
            _playerScore++;
            UpdateScoreUI();
            Debug.Log($"생존 성공! 현재 연속 횟수: {_playerScore} / {requiredScore}");

            if (_playerScore >= requiredScore)
            {
                Debug.Log("스테이지 6 클리어!");

                // 무한 루프 종료 플래그
                _isCleared = true;

                // [핵심 수정] 
                // 모든 타일을 다시 원래 자리로 끌어올리고, 색상은 헷갈리지 않게 전부 회색으로 덮습니다!
                foreach (MatchTile tile in _allTiles)
                {
                    tile.ResetTile(); // 이 함수가 위치 복구 + 콜라이더 활성화 + 회색(HideColor)을 모두 처리합니다.
                }

                // 헷갈리지 않게 전광판도 완전히 꺼버립니다.
                if (displayBoard != null) displayBoard.material.color = Color.black;

                onStageClear?.Invoke();
                UpdateScoreUI();
            }
        }
        else
        {
            _playerScore = 0;
            UpdateScoreUI();
            Debug.Log("추락! 처음부터 다시 생존해야 합니다.");
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreTextMesh == null) return;

        if (_isCleared)
        {
            scoreTextMesh.text = $"{requiredScore}/{requiredScore}";
            scoreTextMesh.color = Color.green;
        }
        else
        {
            scoreTextMesh.text = $"{_playerScore}/{requiredScore}";
        }
    }
}