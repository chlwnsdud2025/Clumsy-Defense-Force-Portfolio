using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spider_Boss : BossEnemy
{
    [Header("Targeting Settings")]
    public LayerMask playerLayerMask; // [추가] 장판(Telegraph) Initialize에 필요한 레이어 마스크

    [Header("Pattern 1: Suicide Spiders (자폭 거미 기습)")]
    public GameObject suicideEnemyPrefab;
    public GameObject spawnWarningVfx;
    public int spiderSpawnCount = 5;
    public float spawnRadius = 8f;
    public float spawnDelay = 1.5f;

    [Header("Pattern 2: Cobweb Trap (플레이어 저격 거미줄 장판)")]
    public GameObject cobwebPrefab;
    public GameObject cobwebTelegraphPrefab;

    [Tooltip("장판 및 거미줄의 반지름(Radius) 크기")]
    public float cobwebRadius = 3.0f; // [핵심] 다른 보스들처럼 반지름 하나로 통일!

    public float cobwebTelegraphDuration = 1.2f;
    public float cobwebLifeTime = 6.0f;

    private Transform cachedVfxContainer = null;

    protected override void Start()
    {
        base.Start();
    }

    protected override void Attack()
    {
        if (isCastingPattern) return;
        isCastingPattern = true;

        if (Random.value > 0.5f)
        {
            StartCoroutine(SpawnSuicideSpidersRoutine());
        }
        else
        {
            StartCoroutine(SpawnCobwebTrapRoutine());
        }
    }

    // [패턴 1] 자폭 거미 기습
    private IEnumerator SpawnSuicideSpidersRoutine()
    {
        if (playerTarget == null)
        {
            isCastingPattern = false;
            yield break;
        }

        Vector3 centerPos = playerTarget.position;
        List<Vector3> validSpawnPoints = new List<Vector3>();
        Transform vfxContainer = GetVfxContainer();

        for (int i = 0; i < spiderSpawnCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 targetPos = centerPos + new Vector3(randomCircle.x, raycastStartHeight, randomCircle.y);

            if (Physics.Raycast(targetPos, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
            {
                validSpawnPoints.Add(hit.point);

                if (spawnWarningVfx != null)
                {
                    GameObject vfx = Instantiate(spawnWarningVfx, hit.point, Quaternion.identity, vfxContainer);

                    Destroy(vfx, spawnDelay + 0.5f);
                }
            }
        }

        yield return new WaitForSeconds(spawnDelay);

        foreach (Vector3 spawnPos in validSpawnPoints)
        {
            if (suicideEnemyPrefab != null)
            {
                GameObject spiderObj = Instantiate(suicideEnemyPrefab, spawnPos, Quaternion.identity, vfxContainer);
                SuicideEnemy spiderScript = spiderObj.GetComponent<SuicideEnemy>();

                if (spiderScript != null)
                {
                    spiderScript.SpawnSetup(playerTarget, spawnPos, EnemyManager.Instance);
                }
            }
        }

        yield return new WaitForSeconds(1.0f);
        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    // [패턴 2] 플레이어 위치에 거미줄 장판 소환
    private IEnumerator SpawnCobwebTrapRoutine()
    {
        if (playerTarget == null)
        {
            isCastingPattern = false;
            yield break;
        }

        Vector3 targetPosition = playerTarget.position;
        Vector3 spawnPosition = targetPosition;
        Vector3 rayStart = targetPosition + Vector3.up * raycastStartHeight;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
        {
            spawnPosition = hit.point;
        }

        Transform vfxContainer = GetVfxContainer();

        // 1. 바닥에 거미줄 예고 장판(Telegraph) 소환
        GameObject telegraphInstance = null;
        if (cobwebTelegraphPrefab != null)
        {
            telegraphInstance = Instantiate(cobwebTelegraphPrefab, spawnPosition, Quaternion.identity, vfxContainer);

            // [수정완료] 다른 보스들처럼 Initialize 함수에 반지름(Radius)을 던져줍니다!
            BossTelegraph script = telegraphInstance.GetComponent<BossTelegraph>();
            if (script != null)
            {
                script.explosionVfxPrefab = null; // 거미줄 장판은 자체 폭발 이펙트 생략
                script.Initialize(0f, cobwebRadius, cobwebTelegraphDuration, playerLayerMask);
            }
        }

        yield return new WaitForSeconds(cobwebTelegraphDuration);
        if (telegraphInstance != null) Destroy(telegraphInstance);

        // 2. 실제 거미줄 오브젝트 생성
        if (cobwebPrefab != null)
        {
            GameObject cobwebObj = Instantiate(cobwebPrefab, spawnPosition + new Vector3(0, 0.7f, 0), Quaternion.identity, vfxContainer);

            // [자동 크기 맞춤] 반지름(Radius) 값을 지름(Radius * 2) 스케일로 변환하여 
            // Cobweb 스크립트가 DOTween을 시작하기 전에 미리 주입해 줍니다.
            cobwebObj.transform.localScale = new Vector3(cobwebRadius * 2.5f, cobwebRadius * 2.5f, cobwebRadius * 2.5f);
            Cobweb cobwebScript = cobwebObj.GetComponent<Cobweb>();
            if (cobwebScript != null)
            {
                cobwebScript.duration = cobwebLifeTime;
            }
        }

 
        yield return new WaitForSeconds(0.5f);
        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    private Transform GetVfxContainer()
    {
        if (cachedVfxContainer != null) return cachedVfxContainer;

        if (EnemyManager.Instance != null && EnemyManager.Instance.FxParent != null)
        {
            cachedVfxContainer = EnemyManager.Instance.FxParent;
        }
        return cachedVfxContainer;
    }
}
