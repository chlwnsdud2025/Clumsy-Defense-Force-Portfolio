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

    private void Start()
    {
        if (EnemyManager.Instance != null && currentTheme != null)
        {
            EnemyManager.Instance.ApplyTheme(currentTheme);
        }

        if (bossMap != null)
        {
            bossMap.position += Vector3.down * mapDropDistance;
            bossMap.gameObject.SetActive(false);
        }
    }

    public void InitializeForRun()
    {
        StartPhase(0);
    }

    public void ReleaseRun()
    {
        StopAllCoroutines();
        currentPhaseIndex = 0;
    }

    private void StartPhase(int phaseIndex)
    {
        if (currentTheme == null || currentTheme.phases == null)
            return;

        if (phaseIndex >= currentTheme.phases.Count)
        {
            Debug.LogError("모든 페이즈 클리어! 최종 보스전 돌입!");
            return;
        }

        Debug.Log("다음 페이즈 시작!");

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
            Debug.Log("6개 스테이지 모두 클리어! 최종 보스 연출을 시작합니다!");
            StartCoroutine(FinalTransitionRoutine());
            return;
        }

        StartPhase(currentPhaseIndex + 1);
    }

    private IEnumerator FinalTransitionRoutine()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && playerTeleportPoint != null)
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

            player.transform.SetParent(bossMap);
        }

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
            plazaMap.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(1f);

        if (bossMap != null)
        {
            bossMap.gameObject.SetActive(true);

            Vector3 bossStartPos = bossMap.position;
            Vector3 bossEndPos = bossStartPos + Vector3.up * mapDropDistance;

            float percent = 0f;
            while (percent < 1f)
            {
                percent += Time.deltaTime / transitionDuration;
                bossMap.position = Vector3.Lerp(bossStartPos, bossEndPos, percent);
                yield return null;
            }
            bossMap.position = bossEndPos;
        }

        if (player != null)
        {
            player.transform.SetParent(null);
        }

        yield return new WaitForSeconds(1f);

        if (finalBossPrefab != null && finalBossSpawnPoint != null)
        {
            Instantiate(finalBossPrefab, finalBossSpawnPoint.position, finalBossSpawnPoint.rotation);
            Debug.Log("최종 보스 강림!!!");
        }
    }

    public WaveEnemy_DataSO GetCurrentWave()
    {
        if (currentTheme == null || currentTheme.phases == null)
            return null;

        if (currentPhaseIndex < 0 || currentPhaseIndex >= currentTheme.phases.Count)
            return null;

        if (!RunManager.TryGetInstance(out RunManager runManager))
            return null;

        WavePhaseTimer waveTimer = runManager.WaveTimer;
        if (waveTimer == null)
            return null;

        return WaveSelection.GetCurrentWave(currentTheme.phases[currentPhaseIndex], waveTimer);
    }
}
