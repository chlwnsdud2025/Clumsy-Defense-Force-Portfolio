using UnityEngine;
using System.Collections; // 코루틴을 위해 추가
using System.Collections.Generic;

public class StageRunManager : MonoBehaviour
{
    public static StageRunManager Instance;

    [Header("Stage Data")]
    public StageTheme_SO currentTheme;
    public Transform midBossSpawnPoint;
    public GameObject[] midBossPrefabs;

    [Header("Runtime Info (디버그용)")]
    public int currentPhaseIndex = 0;
    public float phaseTimeLeft = 300f;
    public float phaseElapsedTime = 0f;
    public float nightmareElapsedTime = 0f;

    public bool isTimerPaused = false;
    public bool isNightmareMode = false;

    [Header("최종 보스 전환 연출 (Final Transition)")]
    [Tooltip("가라앉을 기존 광장 맵의 최상위 부모 오브젝트")]
    public Transform plazaMap;

    [Tooltip("솟아오를 최종 보스 맵의 최상위 부모 오브젝트 (미리 꺼두지 마시고 씬에 배치만 해두세요)")]
    public Transform bossMap;

    [Tooltip("최종 보스 프리팹")]
    public GameObject finalBossPrefab;

    [Tooltip("최종 보스가 스폰될 위치 (보스 맵의 정중앙 등)")]
    public Transform finalBossSpawnPoint;

    [Tooltip("연출이 시작될 때 플레이어가 순간이동할 안전한 위치 (보스 맵 끄트머리 등)")]
    public Transform playerTeleportPoint;

    [Tooltip("맵이 오르내리는 시간 (초)")]
    public float transitionDuration = 4f;

    [Tooltip("맵이 지하로 얼마나 깊이 내려갈지")]
    public float mapDropDistance = 50f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (EnemyManager.Instance != null && currentTheme != null)
        {
            EnemyManager.Instance.ApplyTheme(currentTheme);
        }

        // 시작 시 보스 맵은 보이지 않도록 아래로 내려놓고 꺼둡니다.
        if (bossMap != null)
        {
            bossMap.position += Vector3.down * mapDropDistance;
            bossMap.gameObject.SetActive(false);
        }

        StartPhase(0);
    }

    private void Update()
    {
        if (isTimerPaused) return;

        float dt = Time.deltaTime;
        phaseTimeLeft -= dt;

        if (phaseTimeLeft > 0f)
        {
            phaseElapsedTime += dt;
        }
        else
        {
            if (!isNightmareMode)
            {
                isNightmareMode = true;
                nightmareElapsedTime = 0f;
                Debug.LogWarning("제한 시간 초과! 악몽 웨이브 시작!");
            }
            nightmareElapsedTime += dt;
        }
    }

    private void StartPhase(int phaseIndex)
    {
        if (phaseIndex >= currentTheme.phases.Count)
        {
            Debug.LogError("모든 페이즈 클리어! 최종 보스전 돌입!");
            return;
        }
        Debug.Log("다음 페이즈 시작!");

        currentPhaseIndex = phaseIndex;
        StagePhase currentPhase = currentTheme.phases[currentPhaseIndex];

        phaseTimeLeft = currentPhase.timeLimit;
        phaseElapsedTime = 0f;
        nightmareElapsedTime = 0f;

        isNightmareMode = false;
        isTimerPaused = false;
    }

    public void OnStageCleared()
    {
        isTimerPaused = true;

        if (midBossPrefabs != null && currentPhaseIndex < midBossPrefabs.Length)
        {
            Instantiate(midBossPrefabs[currentPhaseIndex], midBossSpawnPoint.position, Quaternion.identity);
        }
    }

    public void OnMidBossDefeated()
    {
        if (currentPhaseIndex >= 5)
        {
            Debug.Log("6개 스테이지 모두 클리어! 최종 보스 연출을 시작합니다!");

            // [추가됨] 6스테이지 클리어 시 맵 교체 연출 코루틴 실행
            StartCoroutine(FinalTransitionRoutine());
            return;
        }

        StartPhase(currentPhaseIndex + 1);
    }

    // ==========================================
    // [신규 기능] 최종 맵 교체 연출 코루틴
    // ==========================================
    private IEnumerator FinalTransitionRoutine()
    {
        // 1. 플레이어를 솟아오를 보스 맵의 안전지대로 텔레포트
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && playerTeleportPoint != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false; // 텔레포트 전 물리 충돌 끄기

            player.transform.position = playerTeleportPoint.position;
            player.transform.rotation = playerTeleportPoint.rotation;
            // 보스 맵의 자식으로 임시 지정하여 맵과 함께 올라가게 함
            player.transform.SetParent(bossMap);

            if (cc != null) cc.enabled = true;
        }

        // 2. 광장 맵 지하로 가라앉기
        if (plazaMap != null)
        {
            Vector3 plazaStartPos = plazaMap.position;
            Vector3 plazaEndPos = plazaStartPos + Vector3.down * mapDropDistance;

            float percent = 0f;
            while (percent < 1f)
            {
                percent += Time.deltaTime / transitionDuration;
                plazaMap.position = Vector3.Lerp(plazaStartPos, plazaEndPos, percent);
                yield return null;
            }
            plazaMap.gameObject.SetActive(false); // 완전히 내려가면 비활성화
        }

        // 3. 1초 대기 (긴장감 조성)
        yield return new WaitForSeconds(1f);

        // 4. 보스 맵 위로 솟아오르기
        if (bossMap != null)
        {
            bossMap.gameObject.SetActive(true); // 맵 켜기

            Vector3 bossStartPos = bossMap.position;
            Vector3 bossEndPos = bossStartPos + Vector3.up * mapDropDistance; // 위로 올림

            float percent = 0f;
            while (percent < 1f)
            {
                percent += Time.deltaTime / transitionDuration;
                bossMap.position = Vector3.Lerp(bossStartPos, bossEndPos, percent);
                yield return null;
            }
            bossMap.position = bossEndPos; // 오차 보정
        }

        // 5. 플레이어 부모 관계 해제 (다시 월드 종속으로 복구)
        if (player != null)
        {
            player.transform.SetParent(null);
        }

        // 6. 극적인 1초 대기 후 최종 보스 스폰!
        yield return new WaitForSeconds(1f);

        if (finalBossPrefab != null && finalBossSpawnPoint != null)
        {
            Instantiate(finalBossPrefab, finalBossSpawnPoint.position, finalBossSpawnPoint.rotation);
            Debug.Log("최종 보스 강림!!!");

            // (선택) 여기서 브금(BGM)을 최종 보스전 음악으로 바꾸는 코드를 넣으면 완벽합니다!
        }
    }

    public WaveEnemy_DataSO GetCurrentWave()
    {
        StagePhase currentPhase = currentTheme.phases[currentPhaseIndex];
        WaveEnemy_DataSO activeWave = null;

        if (isNightmareMode)
        {
            foreach (WaveEnemy_DataSO wave in currentPhase.nightmareWaves)
            {
                if (wave.waveStartTime <= nightmareElapsedTime) activeWave = wave;
            }
        }
        else
        {
            foreach (WaveEnemy_DataSO wave in currentPhase.phaseWaves)
            {
                if (wave.waveStartTime <= phaseElapsedTime) activeWave = wave;
            }
        }

        return activeWave;
    }
}