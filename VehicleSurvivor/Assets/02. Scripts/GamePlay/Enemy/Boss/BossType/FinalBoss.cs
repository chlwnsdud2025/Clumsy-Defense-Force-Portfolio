using System.Collections;
using UnityEngine;

// 부모 클래스 BossEnemy를 상속받습니다.
public class FinalBoss : BossEnemy
{
    [Header("Meca Boss Targeting")]
    public LayerMask playerLayerMask;

    [Header("Sequence Pattern Assets")]
    public GameObject telegraphPrefab; // BossTelegraph 프리팹

    [Header("Base Attack Settings (공통 데미지/범위)")]
    public float baseDamage = 30f;
    public float baseRadius = 3f;
    public float telegraphDuration = 1.5f;

    [Header("VFX")]
    public GameObject explosionEffectPrefab;
    [Tooltip("보스가 무적일 때 켜질 방어막(쉴드) 이펙트")]
    public GameObject shieldVfx;

    [Header("Final Boss: Statue Mechanics (석상 기믹)")]
    [Tooltip("맵에 배치해 둔 3개의 석상을 순서대로 연결하세요.")]
    public BossStatue[] statues;

    [Tooltip("석상이 켜지는 체력 비율 (예: 0.75, 0.50, 0.25)")]
    public float[] phaseThresholds = { 0.75f, 0.50f, 0.25f };

    [Header("Pattern 0&1: Sequence & Single (연쇄/단일 장판)")]
    public float timeBetweenPhases = 0.5f;

    [Header("Pattern 2: Chase Pattern (연속 추적 장판)")]
    public int trackingCount = 5;          // 쫓아가는 횟수
    public float trackingInterval = 0.8f;  // 장판 생성 간격

    [Header("Pattern 3: Spread Cone Pattern (원뿔 확장 장판)")]
    public int coneRows = 4;             // 몇 개의 행을 만들 것인가
    public float rowForwardSpacing = 5f; // 행 간 앞 방향 간격
    public float circleSideSpacing = 4f; // 행 내의 원 간 옆 방향 간격
    public float coneRowTelegraphDelay = 0.3f; // 각 행이 나타나는 시간차

    [Header("Pattern 4: One Line Pattern (일렬 순차 장판)")]
    public int lineCount = 6;                // 일렬로 몇 개의 장판을 깔 것인가
    public float lineForwardSpacing = 4f;    // 장판 사이의 앞방향 간격
    public float lineTelegraphDelay = 0.2f;  // 장판이 연달아 나타나는 시간차

    // 페이즈 및 무적 관리 변수
    private int currentPhase = 1;      // 1페이즈부터 시작
    private int brokenStatueCount = 0; // 파괴된 석상 누적 개수
    private bool isInvincible = false; // 현재 무적 상태 여부

    private Transform cachedVfxContainer = null;

    protected override void Start()
    {
        // 부모(BossEnemy)의 Start 실행
        base.Start();

        // 맵에 배치된 모든 석상에게 이 보스를 주인으로 등록 (앞서 만든 BossStatue 스크립트 연동)
        if (statues != null)
        {
            foreach (var statue in statues)
            {
                if (statue != null) statue.Setup(this);
            }
        }

        // 시작할 때는 무적 쉴드를 꺼둡니다.
        if (shieldVfx != null) shieldVfx.SetActive(false);
    }

    // ==========================================
    // 🛡️ 무적 메커니즘 및 페이즈 전환 트리거
    // ==========================================
    public override void TakeDamage(float damage)
    {
        // 무적 상태라면 데미지를 받지 않습니다.
        if (isInvincible)
        {
            Debug.Log("보스는 무적 상태입니다! 불이 켜진 석상을 먼저 파괴하세요!");
            return;
        }

        // 부모(BossEnemy)의 데미지 공식을 사용하여 체력을 깎습니다.
        base.TakeDamage(damage);

        // 현재 남은 체력 비율 계산 (0.0 ~ 1.0)
        float healthPercent = currentHealth / maxHealth;

        // 아직 가동할 석상이 남아있고, 체력이 임계점 이하로 떨어졌다면 무적 페이즈 돌입
        if (brokenStatueCount < statues.Length && brokenStatueCount < phaseThresholds.Length)
        {
            if (healthPercent <= phaseThresholds[brokenStatueCount])
            {
                TriggerInvincibilityPhase();
            }
        }
    }

    private void TriggerInvincibilityPhase()
    {
        Debug.Log($"보스: 체력 임계점 도달! {brokenStatueCount + 1}번째 석상 가동 및 무적 전개!");
        isInvincible = true;

        if (shieldVfx != null) shieldVfx.SetActive(true);

        // 현재 순서에 맞는 석상에 불을 켜서 타격 가능하게 변경
        if (statues[brokenStatueCount] != null)
        {
            statues[brokenStatueCount].ActivateStatue();
        }
    }

    // 석상(BossStatue)이 파괴될 때 보스에게 신호를 보내는 함수
    public void OnStatueDestroyed()
    {
        brokenStatueCount++;
        currentPhase++; // 페이즈가 1씩 증가 (1 -> 2 -> 3 -> 4)
        isInvincible = false;

        if (shieldVfx != null) shieldVfx.SetActive(false);

        Debug.Log($"석상 파괴됨! 무적이 해제되었습니다! 현재 페이즈: {currentPhase}");
    }

    // ==========================================
    // ⚔️ 페이즈에 따른 패턴 동적 해금
    // ==========================================
    protected override void Attack()
    {
        // 공격 중이거나 무적 상태일 때는 패턴을 시전하지 않음
        if (isCastingPattern || isInvincible) return;
        isCastingPattern = true;

        // 페이즈에 따라 사용할 수 있는 패턴의 가짓수가 점점 늘어납니다!
        // 1페이즈: Random.Range(0, 2) -> 0, 1 (패턴 2개)
        // 2페이즈: Random.Range(0, 3) -> 0, 1, 2 (패턴 3개)
        // 3페이즈: Random.Range(0, 4) -> 0, 1, 2, 3 (패턴 4개)
        // 4페이즈: Random.Range(0, 5) -> 0, 1, 2, 3, 4 (모든 패턴 개방)
        int maxPatternExclusive = Mathf.Min(currentPhase + 1, 6);
        int randomPattern = Random.Range(0, maxPatternExclusive);

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
    // [기존 코드 유지] 하이어라키 정리용 헬퍼
    // ==========================================
    private Transform GetVfxContainer()
    {
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
    // [기존 코드 유지] 5가지 공격 패턴 코루틴들
    // ==========================================
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

    // ==========================================
    // [기존 코드 유지] 공통 헬퍼 함수 1, 2
    // ==========================================
    private void SpawnOneTelegraph(Vector3 pos)
    {
        if (telegraphPrefab == null) return;

        GameObject go = Instantiate(telegraphPrefab, pos, Quaternion.identity, GetVfxContainer());
        BossTelegraph script = go.GetComponent<BossTelegraph>();
        if (script != null)
        {
            script.explosionVfxPrefab = explosionEffectPrefab;
            script.Initialize(baseDamage, baseRadius, telegraphDuration, playerLayerMask);
        }
    }

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