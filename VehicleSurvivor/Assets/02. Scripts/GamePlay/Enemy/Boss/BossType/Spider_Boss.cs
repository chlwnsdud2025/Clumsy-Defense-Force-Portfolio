using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spider_Boss : BossEnemy
{
    [Header("Pattern: Suicide Spiders (자폭 거미 기습)")]
    [Tooltip("이미 만들어두신 자폭 몹(SuicideEnemy) 프리팹을 연결하세요.")]
    public GameObject suicideEnemyPrefab;

    [Tooltip("몹이 튀어나오기 전에 바닥에 표시될 경고/흙먼지 이펙트")]
    public GameObject spawnWarningVfx;

    public int spiderSpawnCount = 5;       // 한 번에 소환할 몹의 수
    public float spawnRadius = 8f;         // 플레이어 주변 몇 m 반경 내에 소환할지
    public float spawnDelay = 1.5f;        // 이펙트가 생긴 후 몹이 튀어나올 때까지의 시간

    private Transform cachedVfxContainer = null;

    protected override void Start()
    {
        base.Start();
    }

    protected override void Attack()
    {
        if (isCastingPattern) return;
        isCastingPattern = true;

        StartCoroutine(SpawnSuicideSpidersRoutine());
    }

    private Transform GetVfxContainer()
    {
        if (cachedVfxContainer != null) return cachedVfxContainer;
        if (EnemyManager.Instance != null && EnemyManager.Instance.FxParent != null)
        {
            cachedVfxContainer = EnemyManager.Instance.FxParent;
            return cachedVfxContainer;
        }
        GameObject container = GameObject.Find("Boss_VFX_Container");
        if (container == null) container = new GameObject("Boss_VFX_Container");
        cachedVfxContainer = container.transform;
        return cachedVfxContainer;
    }

    // ==========================================
    // 핵심 패턴: 레이캐스트 기반 자폭 몹 기습 소환
    // ==========================================
    private IEnumerator SpawnSuicideSpidersRoutine()
    {
        Debug.Log("거미 보스: 플레이어 주변에 자폭 거미 기습 소환!");

        List<Vector3> validSpawnPoints = new List<Vector3>();

        // 1. 플레이어 주변으로 레이캐스트를 쏴서 소환할 '진짜 바닥'을 찾습니다.
        for (int i = 0; i < spiderSpawnCount; i++)
        {
            // 플레이어 주변 랜덤한 원형 위치 계산
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 rayStartPos = playerTarget.position + new Vector3(randomCircle.x, raycastStartHeight, randomCircle.y);

            // 위에서 아래로 레이저를 쏴서 바닥(groundLayer)을 찾습니다.
            if (Physics.Raycast(rayStartPos, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
            {
                // 허공이나 절벽 밖이 아닌, 안전한 땅바닥 좌표만 리스트에 저장합니다.
                validSpawnPoints.Add(hit.point);

                // 바닥을 찾은 즉시 해당 위치에 흙먼지나 마법진 같은 경고 이펙트를 띄웁니다!
                if (spawnWarningVfx != null)
                {
                    GameObject vfx = Instantiate(spawnWarningVfx, hit.point, Quaternion.identity, GetVfxContainer());
                    Destroy(vfx, spawnDelay + 0.5f); // 몹 소환 후 자동 삭제
                }
            }
        }

        // 2. 이펙트가 터지고 몹이 나올 때까지 대기 (플레이어가 보고 피할/대비할 시간)
        yield return new WaitForSeconds(spawnDelay);

        // 3. 기록해둔 바닥 좌표들에 자폭 몹들을 일제히 스폰합니다.
        foreach (Vector3 spawnPos in validSpawnPoints)
        {
            if (suicideEnemyPrefab != null)
            {
                // 프리팹 소환
                GameObject spiderObj = Instantiate(suicideEnemyPrefab, spawnPos, Quaternion.identity);
                SuicideEnemy spiderScript = spiderObj.GetComponent<SuicideEnemy>();

                if (spiderScript != null)
                {
                    // 개발자님이 만들어두신 기존 함수로 기본 세팅 연동
                    spiderScript.SpawnSetup(playerTarget, spawnPos, EnemyManager.Instance);

                    // [핵심 추가] 보스가 직접 뽑았으니 스스로 Update 돌리라고 스위치를 켜줍니다!
                    spiderScript.EnableSelfUpdate();
                }
            }
        }

        yield return new WaitForSeconds(0.5f);
        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }
}