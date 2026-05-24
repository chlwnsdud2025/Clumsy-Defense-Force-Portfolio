using UnityEngine;
using System.Collections;

public class StageRunManager : MonoSceneSingleton<StageRunManager>
{
    [Header("Stage Data")]
    public StageTheme_SO currentTheme;
    public Transform midBossSpawnPoint;
    public GameObject[] midBossPrefabs;

    [Header("Runtime Info (디버그용)")]
    public int currentPhaseIndex = 0;

    [Header("Debug Options")]
    [Tooltip("체크하고 게임을 시작하면 1~6페이즈를 건너뛰고 바로 최종 포탈을 생성합니다.")]
    public bool debugSkipToFinalBoss = false;

    [Header("Skybox Settings (스카이박스 연출)")]
    [Tooltip("일반 스테이지(광장 맵)에서 사용할 기본 스카이박스 마티리얼")]
    public Material defaultSkybox;

    [Tooltip("최종 보스방에서 사용할 어둡거나 기괴한 보스전 전용 스카이박스 마티리얼")]
    public Material bossSkybox;

    [Header("최종 보스 포탈 연출 (Final Portal)")]
    public GameObject finalPortalPrefab;
    public Transform portalSpawnPoint;

    [Space(10)]
    public GameObject plazaMap;
    public GameObject bossMap;
    public Transform playerTeleportPoint;
    public GameObject finalBossPrefab;
    public Transform finalBossSpawnPoint;

    private void Start()
    {
        if (EnemyManager.Instance != null && currentTheme != null)
        {
            EnemyManager.Instance.ApplyTheme(currentTheme);
        }

        if (bossMap != null)
        {
            bossMap.SetActive(false);
        }

        // 게임 시작 시 기본 스카이박스로 안전하게 세팅
        ApplySkybox(defaultSkybox);
    }

    public void InitializeForRun()
    {
        // 런이 새로 시작될 때 기본 스카이박스로 초기화
        ApplySkybox(defaultSkybox);

        if (debugSkipToFinalBoss)
        {
            Debug.LogWarning("디버그 모드 작동: 모든 페이즈를 스킵하고 최종 보스 포탈을 생성합니다!");
            currentPhaseIndex = 5;
            OnMidBossDefeated();
            return;
        }

        StartPhase(0);
    }

    public void ReleaseRun()
    {
        StopAllCoroutines();
        currentPhaseIndex = 0;
        // 게임 오버나 로비로 나갈 때 스카이박스 복구
        ApplySkybox(defaultSkybox);
    }

    private void StartPhase(int phaseIndex)
    {
        if (currentTheme == null || currentTheme.phases == null)
            return;

        if (phaseIndex >= currentTheme.phases.Count)
        {
            return;
        }

        Debug.Log($"페이즈 {phaseIndex + 1} 시작!");

        currentPhaseIndex = phaseIndex;
        StagePhase currentPhase = currentTheme.phases[currentPhaseIndex];

        if (RunManager.TryGetInstance(out RunManager runManager))
            runManager.StartWavePhase(currentPhase.timeLimit);
    }

    public void OnStageCleared()
    {
        if (midBossPrefabs != null && currentPhaseIndex < midBossPrefabs.Length)
        {
            Instantiate(midBossPrefabs[currentPhaseIndex], midBossSpawnPoint.position, Quaternion.identity);
        }
    }

    public void OnMidBossDefeated()
    {
        if (currentPhaseIndex >= 5)
        {
            Debug.Log("6개 스테이지 모두 클리어 (또는 디버그)! 중앙에 포탈을 생성합니다!");

            if (finalPortalPrefab != null && portalSpawnPoint != null)
            {
                Instantiate(finalPortalPrefab, portalSpawnPoint.position, finalPortalPrefab.transform.rotation);
            }
            return;
        }

        StartPhase(currentPhaseIndex + 1);
    }

    public void TransitionToFinalBoss(GameObject player)
    {
        StartCoroutine(PortalTransitionRoutine(player));
    }

    private IEnumerator PortalTransitionRoutine(GameObject player)
    {
        if (plazaMap != null) plazaMap.SetActive(false);
        if (bossMap != null) bossMap.SetActive(true);

        // ==========================================
        // 🌌 [핵심 추가] 포탈을 타는 순간 보스전 스카이박스로 변경!
        // ==========================================
        ApplySkybox(bossSkybox);

        if (playerTeleportPoint != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.Teleport(playerTeleportPoint.position, playerTeleportPoint.rotation);
            }
            else
            {
                player.transform.SetPositionAndRotation(playerTeleportPoint.position, playerTeleportPoint.rotation);
            }
        }

        yield return new WaitForSeconds(1.0f);

        if (finalBossPrefab != null && finalBossSpawnPoint != null)
        {
            Instantiate(finalBossPrefab, finalBossSpawnPoint.position, finalBossSpawnPoint.rotation);
            Debug.Log("최종 보스 강림!!!");
        }
    }

    public WaveEnemy_DataSO GetCurrentWave()
    {
        if (currentTheme == null || currentTheme.phases == null) return null;
        if (currentPhaseIndex < 0 || currentPhaseIndex >= currentTheme.phases.Count) return null;
        if (!RunManager.TryGetInstance(out RunManager runManager)) return null;

        WavePhaseTimer waveTimer = runManager.WaveTimer;
        if (waveTimer == null) return null;

        return WaveSelection.GetCurrentWave(currentTheme.phases[currentTheme.phases.Count - 1], waveTimer);
    }

    [ContextMenu("디버그: 최종 포탈 즉시 생성")]
    public void DebugSpawnFinalPortal()
    {
        Debug.LogWarning("인스펙터 강제 명령: 최종 보스 포탈을 즉시 생성합니다!");

        if (RunManager.TryGetInstance(out RunManager runManager))
        {
            runManager.StopAllCoroutines();
        }

        currentPhaseIndex = 5;
        OnMidBossDefeated();
    }

    // ==========================================
    // 스카이박스 교체 및 라이팅 환경 갱신 헬퍼 함수
    // ==========================================
    private void ApplySkybox(Material skyboxMaterial)
    {
        if (skyboxMaterial == null) return;

        // 1. 유니티 렌더 설정의 스카이박스 변경
        RenderSettings.skybox = skyboxMaterial;

        // 2. 실시간 환경광(글로벌 일루미네이션)을 사용하는 경우, 스카이박스 변경에 맞춰 주변 빛 색감도 동적으로 갱신해줍니다.
        DynamicGI.UpdateEnvironment();
    }
}