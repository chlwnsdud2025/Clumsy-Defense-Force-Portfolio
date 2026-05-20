using UnityEngine;
using System.Collections.Generic;

public class StageRunManager : MonoBehaviour
{
    public static StageRunManager Instance;

    [Header("Stage Data")]
    public StageTheme_SO currentTheme;
    public Transform midBossSpawnPoint;
    public GameObject[] midBossPrefabs; // 6개의 보스 순서대로 등록

    [Header("Runtime Info (디버그용)")]
    public int currentPhaseIndex = 0;
    public float phaseTimeLeft = 300f;
    public float phaseElapsedTime = 0f;
    public float nightmareElapsedTime = 0f;

    public bool isTimerPaused = false;
    public bool isNightmareMode = false;

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

        currentPhaseIndex = phaseIndex;
        StagePhase currentPhase = currentTheme.phases[currentPhaseIndex];

        phaseTimeLeft = currentPhase.timeLimit;
        phaseElapsedTime = 0f;
        nightmareElapsedTime = 0f;

        isNightmareMode = false;
        isTimerPaused = false;
    }

    // 스테이지 끝의 목표물을 건드렸을 때
    public void OnStageCleared()
    {
        isTimerPaused = true;

        if (midBossPrefabs != null && currentPhaseIndex < midBossPrefabs.Length)
        {
            Instantiate(midBossPrefabs[currentPhaseIndex], midBossSpawnPoint.position, Quaternion.identity);
        }
    }

    // 보스가 죽었을 때 보스 스크립트(Die)에서 호출
    public void OnMidBossDefeated()
    {
        if (currentPhaseIndex >= 5)
        {
            Debug.Log("6개 스테이지 모두 클리어! 최종 씬 전환 등 연출 추가");
            return;
        }

        StartPhase(currentPhaseIndex + 1);
    }

    // 스포너가 몹을 뽑을 때 이 함수로 웨이브 정보를 받아감
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