using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    
    [Header("Treasure Chest System")]
    public GameObject chestPrefab;
    [Tooltip("맵에 고정으로 소환될 5개의 상자 위치를 지정하세요.")]
    public Transform[] fixedChestSpawnPoints;

    [Tooltip("상자를 처음 열 때 필요한 골드")]
    public int initialChestCost = 100;

    [Tooltip("상자를 열 때마다 증가할 비용 퍼센트 (예: 50을 넣으면 50% 증가)")]
    public float chestCostIncreasePercent = 50f;

    [Tooltip("상자가 열려 파괴된 후, 해당 자리에 다시 새 상자가 스폰되기까지 걸리는 시간(초)")]
    public float chestRespawnDelay = 15f;

    public int CurrentChestCost { get; private set; }

    public event System.Action OnChestCostChanged;

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
        isBossActive = false;

        InitializeChestSystem();

        if (debugSkipToFinalBoss)
        {
            Debug.LogWarning("디버그 모드 작동: 모든 페이즈를 스킵하고 최종 보스 포탈을 생성합니다!");
            currentPhaseIndex = 5;
            OnMidBossDefeated();
            return;
        }

        StartPhase(0);
    }

    private void InitializeChestSystem()
    {
        CurrentChestCost = initialChestCost;
        OnChestCostChanged?.Invoke();

        TreasureChest[] existingChests = FindObjectsOfType<TreasureChest>();
        foreach (var chest in existingChests)
        {
            Destroy(chest.gameObject);
        }

        if (chestPrefab != null && fixedChestSpawnPoints != null)
        {
            foreach (Transform spawnPoint in fixedChestSpawnPoints)
            {
                if (spawnPoint != null)
                {
                    Instantiate(chestPrefab, spawnPoint.position, spawnPoint.rotation);
                }
            }
        }
    }

    //상자가 열렸을 때 일괄적으로 이벤트를 처리하는 단일 창구 함수
    public void HandleChestOpened(Vector3 position, Quaternion rotation)
    {
        // 1. 퍼센트 비용 복리 증가 규칙 적용
        IncreaseChestCost();

        // 2. 일정 시간 뒤 원래 위치에 완벽히 새 상자를 배치하는 코루틴 타이머 가동
        StartCoroutine(RespawnChestRoutine(position, rotation));
    }

    private void IncreaseChestCost()
    {
        float multiplier = 1f + (chestCostIncreasePercent / 100f);
        CurrentChestCost = Mathf.RoundToInt(CurrentChestCost * multiplier);
        OnChestCostChanged?.Invoke();
    }

    // 정해진 시간 대기 후 상자를 월드에 새로 생성하는 리스폰 코루틴
    private IEnumerator RespawnChestRoutine(Vector3 position, Quaternion rotation)
    {
        yield return new WaitForSeconds(chestRespawnDelay);

        if (chestPrefab != null)
        {
            Instantiate(chestPrefab, position, rotation);
            Debug.Log($"[상자 리스폰] 파괴되었던 좌표({position})에 새 상자가 재소환되었습니다. 현재 비용: {CurrentChestCost}");
        }
    }

    public void SpawnBossChest(Vector3 dropPosition)
    {
        if (chestPrefab != null)
        {
            Instantiate(chestPrefab, dropPosition, Quaternion.identity);
            Debug.Log($"보스 처치 보상으로 상자가 드롭되었습니다! (오픈 비용: {CurrentChestCost})");
        }
    }

    public void ReleaseRun()
    {
        // 게임이 종료되거나 재시작될 때, 뒤에서 대기 중이던 리스폰 코루틴들을 싹 날려 메모리 누수 및 오작동을 원천 차단합니다.
        StopAllCoroutines();
        currentPhaseIndex = 0;
        isBossActive = false;
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
        StagePhase_SO currentPhase = currentTheme.phases[currentPhaseIndex];

        if (RunManager.TryGetInstance(out RunManager runManager))
            runManager.StartWavePhase(currentPhase.timeLimit);
    }

    public void OnStageCleared()
    {
        if (isBossActive)
        {
            Debug.LogWarning($"[차단] 현재 {currentPhaseIndex + 1}페이즈 보스가 이미 살아있습니다. 중복 소환이 불가능합니다.");
            return;
        }

        if (midBossPrefabs != null && currentPhaseIndex < midBossPrefabs.Length)
        {
            isBossActive = true;
            Instantiate(midBossPrefabs[currentPhaseIndex], midBossSpawnPoint.position, Quaternion.identity);
            Debug.Log($"[시스템 잠금] {currentPhaseIndex + 1}페이즈 중간 보스 출현!");
        }
    }

    public void OnMidBossDefeated()
    {
        isBossActive = false;
        Debug.Log($"[시스템 잠금 해제] 중간 보스 처치 완료!");

        if (currentPhaseIndex >= 5)
        {
            Debug.Log("6번째 보스 처치! 다음 페이즈로 바로 넘어가지 않고 포탈을 생성합니다!");

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
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.ClearAllActiveEnemies();
            Debug.Log("필드에 남은 모든 몹을 풀로 회수했습니다.");
        }

        if (plazaMap != null) plazaMap.SetActive(false);
        if (bossMap != null) bossMap.SetActive(true);

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

        StartPhase(currentPhaseIndex + 1);

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

    public StagePhase_SO CurrentPhase
    {
        get
        {
            if (currentTheme == null || currentTheme.phases == null) return null;
            if (currentPhaseIndex < 0 || currentPhaseIndex >= currentTheme.phases.Count) return null;
            return currentTheme.phases[currentPhaseIndex];
        }
    }

    /// <summary>현재 페이즈의 적 배율. 테마/페이즈가 없으면 1배.</summary>
    public EnemyScaling CurrentEnemyScaling => EnemyScaling.From(CurrentPhase);

    /// <summary>씬에 StageRunManager 가 없으면(벤치마크 씬 등) 1배를 돌려준다.</summary>
    public static EnemyScaling CurrentEnemyScalingOrIdentity =>
        HasInstance ? Instance.CurrentEnemyScaling : EnemyScaling.Identity;

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
        isBossActive = false;
        OnMidBossDefeated();
    }

    private void ApplySkybox(Material skyboxMaterial)
    {
        if (skyboxMaterial == null) return;
        RenderSettings.skybox = skyboxMaterial;
        DynamicGI.UpdateEnvironment();
    }
}