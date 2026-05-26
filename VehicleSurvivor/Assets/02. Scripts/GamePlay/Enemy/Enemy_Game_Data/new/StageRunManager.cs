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
    public Material defaultSkybox;
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

    [Header("최종 보스전 씬 오브젝트")]
    [Tooltip("씬에 이미 배치되어 있는 석상 3개를 순서대로 드래그해서 넣으세요.")]
    public BossStatue[] sceneStatues;

    // ==========================================
    // 🔒 [핵심 추가] 현재 중간 보스가 맵에 활성화되어 있는지 추적하는 상태 변수
    // ==========================================
    public bool isBossActive { get; private set; } = false;

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

        ApplySkybox(defaultSkybox);
    }

    public void InitializeForRun()
    {
        ApplySkybox(defaultSkybox);
        isBossActive = false; // 런 시작 시 초기화

        if (debugSkipToFinalBoss)
        {
            Debug.LogWarning("디버그 모드 작동: 모든 페이즈를 스킵하고 최종 보스 포탈을 생성합니다!");
            currentPhaseIndex = 5; // 6번째 보스를 잡은 상태로 세팅
            OnMidBossDefeated();   // 포탈 소환
            return;
        }

        StartPhase(0);
    }

    public void ReleaseRun()
    {
        StopAllCoroutines();
        currentPhaseIndex = 0;
        isBossActive = false; // 런이 종료되거나 취소될 때 락 해제
        ApplySkybox(defaultSkybox);
    }

    private void StartPhase(int phaseIndex)
    {
        if (currentTheme == null || currentTheme.phases == null)
            return;

        if (phaseIndex >= currentTheme.phases.Count)
            return;

        Debug.Log($"페이즈 {phaseIndex + 1} 시작!");

        currentPhaseIndex = phaseIndex;
        StagePhase currentPhase = currentTheme.phases[currentPhaseIndex];

        // 인스펙터에 설정된 timeLimit(최종 보스전은 600)으로 타이머가 알아서 세팅됩니다!
        if (RunManager.TryGetInstance(out RunManager runManager))
            runManager.StartWavePhase(currentPhase.timeLimit);
    }

    public void OnStageCleared()
    {
        // ==========================================
        // 🔒 [방어 추가] 이미 보스가 소환되어 진행 중이라면 중복 소환을 차단합니다!
        // ==========================================
        if (isBossActive)
        {
            Debug.LogWarning($"[차단] 현재 {currentPhaseIndex + 1}페이즈 보스가 이미 살아있습니다. 중복 소환이 불가능합니다.");
            return;
        }

        if (midBossPrefabs != null && currentPhaseIndex < midBossPrefabs.Length)
        {
            isBossActive = true; // 중간 보스 활성화 상태 잠금
            Instantiate(midBossPrefabs[currentPhaseIndex], midBossSpawnPoint.position, Quaternion.identity);
            Debug.Log($"[시스템 잠금] {currentPhaseIndex + 1}페이즈 중간 보스 출현!");
        }
    }

    public void OnMidBossDefeated()
    {
        // ==========================================
        // 🔒 [락 해제] 보스가 처치되었으므로 상태를 초기화하여 시스템 잠금을 해제합니다.
        // ==========================================
        isBossActive = false;
        Debug.Log($"[시스템 잠금 해제] 중간 보스 처치 완료!");

        // 6번째 보스(인덱스 5) 격파 시
        if (currentPhaseIndex >= 5)
        {
            Debug.Log("6번째 보스 처치! 다음 페이즈로 바로 넘어가지 않고 포탈을 생성합니다!");

            // (선택 사항) 포탈을 타기 전까지 잡몹이 안 나오게 하려면 여기서 타이머를 일시정지 해도 좋습니다.
            // if (RunManager.TryGetInstance(out RunManager rm)) rm.WaveTimer.PauseTimer();

            if (finalPortalPrefab != null && portalSpawnPoint != null)
            {
                Instantiate(finalPortalPrefab, portalSpawnPoint.position, finalPortalPrefab.transform.rotation);
            }
            // 여기서 return 하여 StartPhase가 호출되지 않게 막습니다! (포탈 대기 상태)
            return;
        }

        // 1~5번째 보스라면 처치 즉시 다음 페이즈로 정상 진행
        StartPhase(currentPhaseIndex + 1);
    }

    public void TransitionToFinalBoss(GameObject player)
    {
        StartCoroutine(PortalTransitionRoutine(player));
    }

    private IEnumerator PortalTransitionRoutine(GameObject player)
    {
        // 맵을 끄기 전에 필드에 남은 모든 적을 풀로 되돌려 보냅니다!
        if (EnemyManager.Instance != null)
        {
            // 개발자님의 EnemyManager 스크립트에 있는 '전체 회수' 함수를 호출합니다.
            EnemyManager.Instance.ClearAllActiveEnemies();
            Debug.Log("필드에 남은 모든 몹을 풀로 회수했습니다.");
        }

        // 1. 맵 전환
        if (plazaMap != null) plazaMap.SetActive(false);
        if (bossMap != null) bossMap.SetActive(true);

        ApplySkybox(bossSkybox);

        // 2. 플레이어 순간이동
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

        // 3. 마지막 7번째 페이즈(10분) 진입
        StartPhase(currentPhaseIndex + 1);

        // 4. 최종 보스 소환
        if (finalBossPrefab != null && finalBossSpawnPoint != null)
        {
            GameObject bossObj = Instantiate(finalBossPrefab, finalBossSpawnPoint.position, finalBossSpawnPoint.rotation);
            FinalBoss finalBoss = bossObj.GetComponent<FinalBoss>();

            if (finalBoss != null)
            {
                finalBoss.InitializeStatues(sceneStatues);
            }

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

        return WaveSelection.GetCurrentWave(currentTheme.phases[currentPhaseIndex], waveTimer);
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
        isBossActive = false; // 디버그 소환 시에도 안전하게 해제
        OnMidBossDefeated();
    }

    private void ApplySkybox(Material skyboxMaterial)
    {
        if (skyboxMaterial == null) return;
        RenderSettings.skybox = skyboxMaterial;
        DynamicGI.UpdateEnvironment();
    }
}