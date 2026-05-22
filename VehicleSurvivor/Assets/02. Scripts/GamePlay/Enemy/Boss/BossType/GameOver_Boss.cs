using System.Collections;
using UnityEngine;

// 부모 클래스 BossEnemy를 상속받습니다.
public class GameOver_Boss : BossEnemy
{
    [Header("Meca Boss Targeting")]
    public LayerMask playerLayerMask;

    [Header("Sequence Pattern Assets")]
    public GameObject telegraphPrefab; // BossTelegraph 프리팹

    [Header("Base Attack Settings (공통 데미지/범위)")]
    // 모든 패턴이 기본적으로 공유하는 데미지와 크기입니다.
    public float baseDamage = 30f;
    public float baseRadius = 3f;
    public float telegraphDuration = 1.5f;

    [Header("VFX")]
    public GameObject explosionEffectPrefab;

    [Header("Pattern 0&1: Sequence & Single (연쇄/단일 장판)")]
    public float timeBetweenPhases = 0.5f;

    [Header("Pattern 2: Chase Pattern (연속 추적 장판)")]
    public int trackingCount = 5;          // 쫓아가는 횟수
    public float trackingInterval = 0.8f;  // 장판 생성 간격

    [Header("Pattern 3: Spread Cone Pattern (원뿔 확장 장판)")]
    public int coneRows = 4;            // 몇 개의 행을 만들 것인가
    public float rowForwardSpacing = 5f; // 행 간 앞 방향 간격
    public float circleSideSpacing = 4f; // 행 내의 원 간 옆 방향 간격
    public float coneRowTelegraphDelay = 0.3f; // 각 행이 나타나는 시간차


    [Header("Pattern 4: One Line Pattern (일렬 순차 장판)")]
    public int lineCount = 6;                // 일렬로 몇 개의 장판을 깔 것인가
    public float lineForwardSpacing = 4f;    // 장판 사이의 앞방향 간격
    public float lineTelegraphDelay = 0.2f;  // 장판이 연달아 나타나는 시간차


    private Transform cachedVfxContainer = null;
    // 부모의 abstract 함수를 강제로 덮어써서 자신만의 패턴을 실행합니다.
    protected override void Attack()
    {
        if (isCastingPattern) return;
        isCastingPattern = true;

        int randomPattern = Random.Range(0, 5);

        switch (randomPattern)
        {
            case 0:
                StartCoroutine(SpawnSequencedAttack());
                break;
            case 1:
                StartCoroutine(SpawnOneCircleAttack());
                break;
            case 2:
                StartCoroutine(SpawnChaseCircleAttack());
                break;
            case 3:
                StartCoroutine(SpawnSpreadConeAttack());
                break;
            case 4:
                StartCoroutine(SpawnOneLineAttack());
                break;
        }
    }

    // ==========================================
    // [핵심 추가] 하이어라키를 깨끗하게 만들어줄 전용 폴더 생성기
    // ==========================================
    private Transform GetVfxContainer()
    {
        // 1. 이미 찾은 적이 있다면 검색하지 않고 바로 반환! (초고속)
        if (cachedVfxContainer != null) return cachedVfxContainer;

        if (EnemyManager.Instance != null && EnemyManager.Instance.FxParent != null)
        {
            cachedVfxContainer = EnemyManager.Instance.FxParent;
            return cachedVfxContainer;
        }

        GameObject container = GameObject.Find("Boss_VFX_Container");
        if (container == null)
        {
            container = new GameObject("Boss_VFX_Container");
        }

        cachedVfxContainer = container.transform;
        return cachedVfxContainer;
    }
    // ==========================================

    // 패턴 0: 플레이어를 중심으로 퍼져나가는 연쇄 장판
    private IEnumerator SpawnSequencedAttack()
    {
        Debug.Log("MecaBoss: 연쇄 장판 시전 시작!");

        Vector3 originPos = playerTarget.transform.position;
        originPos.y = transform.position.y;

        SpawnOneTelegraph(originPos);
        yield return new WaitForSeconds(timeBetweenPhases);

        SpawnRingTelegraphs(originPos, 7f, 6);
        yield return new WaitForSeconds(timeBetweenPhases);

        SpawnRingTelegraphs(originPos, 14f, 12);

        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    // 패턴 1: 단일 장판 1회
    private IEnumerator SpawnOneCircleAttack()
    {
        Debug.Log("MecaBoss: 단일 장판 시전 시작!");

        Vector3 originPos = playerTarget.transform.position;
        originPos.y = transform.position.y;

        SpawnOneTelegraph(originPos);

        yield return new WaitForSeconds(timeBetweenPhases);

        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    // 패턴 2: 플레이어를 따라다니는 연속 추적 장판
    private IEnumerator SpawnChaseCircleAttack()
    {
        Debug.Log($"MecaBoss: 연속 추적 장판 {trackingCount}연발 시전 시작!");

        for (int i = 0; i < trackingCount; i++)
        {
            Vector3 currentPos = playerTarget.transform.position;
            currentPos.y = transform.position.y;

            SpawnOneTelegraph(currentPos);
            yield return new WaitForSeconds(trackingInterval);
        }

        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    // 패턴 3: 보스 발밑에서 플레이어를 향해 확장되는 원뿔 장판
    private IEnumerator SpawnSpreadConeAttack()
    {
        Debug.Log($"MecaBoss: 원뿔 확장 장판 시전 시작!");

        Vector3 bossPos = transform.position;
        Vector3 targetPos = playerTarget.position;
        Vector3 forwardDir = (targetPos - bossPos).normalized;
        forwardDir.y = 0;

        Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;

        Vector3 coneApexPos = bossPos + (forwardDir * 1f);
        coneApexPos.y = bossPos.y;

        for (int row = 0; row < coneRows; row++)
        {
            int circlesInRow = row + 1;
            Vector3 rowCenterPos = coneApexPos + (forwardDir * row * rowForwardSpacing);

            for (int j = 0; j < circlesInRow; j++)
            {
                float sideMultiplier = j - (circlesInRow - 1) / 2.0f;
                Vector3 spawnPos = rowCenterPos + (rightDir * sideMultiplier * circleSideSpacing);

                SpawnOneTelegraph(spawnPos);
            }
            yield return new WaitForSeconds(coneRowTelegraphDelay);
        }

        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    // 패턴 4: 보스 발밑에서 플레이어를 향해 일렬로 생성되는 단일 장판
    private IEnumerator SpawnOneLineAttack()
    {
        Debug.Log($"MecaBoss: 단일 일렬 순차 장판 시전 시작!");

        Vector3 bossPos = transform.position;
        Vector3 targetPos = playerTarget.position;
        Vector3 forwardDir = (targetPos - bossPos).normalized;
        forwardDir.y = 0;

        Vector3 startPos = bossPos + (forwardDir * 1.5f);
        startPos.y = bossPos.y;

        for (int i = 0; i < lineCount; i++)
        {
            Vector3 spawnPos = startPos + (forwardDir * (i * lineForwardSpacing));

            SpawnOneTelegraph(spawnPos);

            yield return new WaitForSeconds(lineTelegraphDelay);
        }

        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    // 공통 헬퍼 함수 1: 구체 장판 하나 소환
    private void SpawnOneTelegraph(Vector3 pos)
    {
        if (telegraphPrefab == null) return;

        // [핵심 수정] 무조건 전용 폴더(GetVfxContainer()) 안으로 집어넣습니다!
        GameObject go = Instantiate(telegraphPrefab, pos, Quaternion.identity, GetVfxContainer());
        BossTelegraph script = go.GetComponent<BossTelegraph>();
        if (script != null)
        {
            // 보스의 이펙트 프리팹을 장판에 전달
            script.explosionVfxPrefab = explosionEffectPrefab;
            script.Initialize(baseDamage, baseRadius, telegraphDuration, playerLayerMask);
        }
    }

    // 공통 헬퍼 함수 2: 원형 순차 구체장판 위치 계산
    private void SpawnRingTelegraphs(Vector3 center, float ringRadius, int count)
    {
        float angleStep = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float angleRad = (i * angleStep) * Mathf.Deg2Rad;
            float spawnX = center.x + (ringRadius * Mathf.Cos(angleRad));
            float spawnZ = center.z + (ringRadius * Mathf.Sin(angleRad));

            SpawnOneTelegraph(new Vector3(spawnX, center.y, spawnZ));
        }
    }
}