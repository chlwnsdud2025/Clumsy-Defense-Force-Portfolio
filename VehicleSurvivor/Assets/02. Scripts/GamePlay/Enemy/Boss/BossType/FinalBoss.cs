using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class FinalBoss : BossEnemy
{
    [Header("Meca Boss Targeting")]
    public LayerMask playerLayerMask;

    [Header("Common Pattern Assets")]
    public GameObject telegraphPrefab; // 공용 예고 장판

    [Header("Final Boss: Statue Mechanics (석상 무적 기믹)")]
    public BossStatue[] statues;
    public float[] phaseThresholds = { 0.75f, 0.50f, 0.25f };
    public GameObject shieldVfx;
    private GameObject activeShieldInstance;

    // ==========================================
    // ⏱️ [핵심 추가] 2페이즈 이상 추가 패턴 전용 쿨타임 시스템
    // ==========================================
    [Header("Phase 2+ Extra Attack Settings")]
    [Tooltip("1페이즈 패턴과 별개로 돌아가는 2페이즈 이상 패턴들의 독립 쿨타임")]
    public float extraAttackCooltime = 6.0f;
    private float currentExtraAttackCooldown;
    private bool isCastingExtraPattern = false;

    // ==========================================
    // 🔴 1페이즈 패턴 설정
    // ==========================================
    [Header("Phase 1: Spreading Ring (원형 연쇄 확장)")]
    public GameObject sequencedVfx;
    public float seqDamage = 40f;
    public float seqRadius = 3.5f;
    public float seqDuration = 1.5f;
    public float timeBetweenPhases = 0.5f;

    [Header("Phase 1: Spread Cone (원뿔 확장 장판)")]
    public GameObject spreadConeVfx;
    public float coneDamage = 25f;
    public float coneRadius = 2.5f;
    public float coneDuration = 1.2f;
    public int coneRows = 4;
    public float rowForwardSpacing = 5f;
    public float circleSideSpacing = 4f;
    public float coneRowTelegraphDelay = 0.3f;

    [Header("Phase 1: One Line (직선 일렬 장판)")]
    public GameObject oneLineVfx;
    public float lineDamage = 35f;
    public float lineRadius = 3f;
    public float lineDuration = 1.3f;
    public int lineCount = 6;
    public float lineForwardSpacing = 4f;
    public float lineTelegraphDelay = 0.2f;

    // ==========================================
    // 🟠 2페이즈 패턴 설정
    // ==========================================
    [Header("Phase 2: Chase Circle (쌍둥이 퀸 연속 추적)")]
    public GameObject chaseVfx;
    public float chaseDamage = 30f;
    public float chaseRadius = 3f;
    public float chaseDuration = 1.5f;
    public int trackingCount = 5;
    public float trackingInterval = 0.8f;

    [Header("Phase 2: Multi Laser (꿀벌 연속 레이저)")]
    public int multiLaserCount = 3;
    public float multiLaserDamage = 30f;
    public float multiLaserLength = 30f;
    public float multiLaserChargeTime = 0.5f;
    public float multiLaserDelay = 0.2f;

    // ==========================================
    // 🟡 3페이즈 패턴 설정
    // ==========================================
    [Header("Phase 3: Shotgun Laser (전갈 산탄 레이저)")]
    public int shotgunPelletCount = 15;
    public float spreadAngle = 40f;
    public float shotgunLaserDamage = 30f;
    public float shotgunLaserLength = 30f;
    public float shotgunLaserChargeTime = 1.2f;

    [Header("Common Laser Assets (2, 3페이즈 공용)")]
    public Transform firePoint;
    public GameObject laserIndicatorPrefab;
    public GameObject laserWarningVfx;
    public GameObject laserFireVfx;
    public float laserStartRadius = 3.0f;
    public float laserEndRadius = 0.3f;
    public float laserCoreRadius = 0.3f;

    [Header("Phase 3: Slow Large Circle (볼케이노 강한 한방)")]
    public GameObject bigCircleVfx;
    public float bigCircleDamage = 60f;
    public float bigCircleRadius = 8f;
    public float bigCircleDuration = 3.0f;

    // ==========================================
    // 🟣 4페이즈 패턴 설정 (발악기)
    // ==========================================
    [Header("Phase 4: Suicide Spiders (거미 잡몹 소환)")]
    public GameObject suicideEnemyPrefab;
    public GameObject spiderSpawnWarningVfx;
    public int spiderSpawnCount = 5;
    public float spiderSpawnRadius = 8f;
    public float spiderSpawnDelay = 1.5f;

    [Header("Phase 4: Teleport Strike (팬텀 순간이동 강타)")]
    public GameObject bossModel;
    public GameObject disappearVfxPrefab;
    public GameObject appearVfxPrefab;
    public float teleportDamage = 40f;
    public float teleportRadius = 4.0f;
    public float teleportDuration = 1.5f;
    public float disappearAnimTime = 0.3f;
    public float appearAnimTime = 0.3f;

    // 내부 상태 관리 변수
    private int currentPhase = 1;
    private int brokenStatueCount = 0;
    private bool isInvincible = false;

    private Transform cachedVfxContainer = null;
    private Collider bossCollider;
    private Vector3 originalModelScale;
    private Vector3 originalModelLocalPos;

    protected override void Start()
    {
        base.Start();

        // [추가] 2페이즈 전용 쿨타임 초기화
        currentExtraAttackCooldown = extraAttackCooltime;

        bossCollider = GetComponent<Collider>();
        if (bossModel != null)
        {
            originalModelScale = bossModel.transform.localScale;
            originalModelLocalPos = bossModel.transform.localPosition;
        }
    }

    // ==========================================
    // ⏱️ [핵심 추가] 두 개의 쿨타임을 동시에 돌리는 Update 오버라이드
    // ==========================================
    protected override void Update()
    {
        // 부모(BossEnemy)의 Update를 실행합니다. 
        // 여기서 이동, 충돌 체크, 그리고 [1페이즈 기본 패턴(Attack)]의 쿨타임이 독립적으로 돌아갑니다.
        base.Update();

        if (playerTarget == null) return;

        // [추가 패턴 쿨타임] 2페이즈 이상일 때, 별개의 쿨타임을 돌려서 동시 다발적인 공격을 수행합니다.
        if (currentPhase >= 2 && !isCastingExtraPattern)
        {
            currentExtraAttackCooldown -= Time.deltaTime;
            if (currentExtraAttackCooldown <= 0)
            {
                ExtraAttack(); // 추가 패턴 발동!
            }
        }
    }

    // ==========================================
    // 🛡️ 무적 및 석상 파괴 (페이즈 관리)
    // ==========================================
    public override void TakeDamage(DamageInfo damageInfo)
    {
        if (isInvincible) return;

        base.TakeDamage(damageInfo);

        float healthPercent = currentHealth / maxHealth;

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
        isInvincible = true;

        if (shieldVfx != null && activeShieldInstance == null)
        {
            activeShieldInstance = Instantiate(shieldVfx, transform.position, transform.rotation, transform);
        }

        if (statues[brokenStatueCount] != null) statues[brokenStatueCount].ActivateStatue();
    }

    public void OnStatueDestroyed()
    {
        brokenStatueCount++;
        currentPhase++;
        isInvincible = false;

        if (activeShieldInstance != null)
        {
            Destroy(activeShieldInstance);
            activeShieldInstance = null;
        }
    }

    // ==========================================
    // ⚔️ [1페이즈] 기본 패턴 전담 (부모의 Attack 쿨타임을 따름)
    // ==========================================
    protected override void Attack()
    {
        if (isCastingPattern) return;
        isCastingPattern = true;

        // 이제 Attack()은 1페이즈 패턴(0, 1, 2)만 전담하여 쿨타임을 돌립니다.
        int selectedPattern = Random.Range(0, 3);

        switch (selectedPattern)
        {
            case 0: StartCoroutine(SpawnSequencedAttack()); break;
            case 1: StartCoroutine(SpawnSpreadConeAttack()); break;
            case 2: StartCoroutine(SpawnOneLineAttack()); break;
        }
    }

    // ==========================================
    // ⚔️ [2페이즈 이상] 추가 패턴 전담 (독립된 Extra 쿨타임을 따름)
    // ==========================================
    private void ExtraAttack()
    {
        isCastingExtraPattern = true;

        List<int> patternPool = new List<int>();

        if (currentPhase >= 2) { patternPool.Add(3); patternPool.Add(4); }
        if (currentPhase >= 3) { patternPool.Add(5); patternPool.Add(6); }
        if (currentPhase >= 4) { patternPool.Add(7); patternPool.Add(8); }

        if (patternPool.Count == 0)
        {
            isCastingExtraPattern = false;
            return;
        }

        int randomIndex = Random.Range(0, patternPool.Count);
        int selectedPattern = patternPool[randomIndex];

        switch (selectedPattern)
        {
            case 3: StartCoroutine(SpawnChaseCircleAttack()); break;
            case 4: StartCoroutine(MultiStingerLaserRoutine()); break;
            case 5: StartCoroutine(ShotgunLaserRoutine()); break;
            case 6: StartCoroutine(SlowLargeCircleRoutine()); break;
            case 7: StartCoroutine(SpawnSuicideSpidersRoutine()); break;
            case 8: StartCoroutine(TeleportStrikeRoutine()); break;
        }
    }

    // ==========================================
    // 🔴 1페이즈 코루틴들 (기본 쿨타임 변수 해제)
    // ==========================================
    private IEnumerator SpawnSequencedAttack()
    {
        Vector3 originPos = playerTarget.position;
        SpawnOneTelegraph(originPos, sequencedVfx, seqDamage, seqRadius, seqDuration);
        yield return new WaitForSeconds(timeBetweenPhases);
        SpawnRingTelegraphs(originPos, 7f, 6, sequencedVfx, seqDamage, seqRadius, seqDuration);
        yield return new WaitForSeconds(timeBetweenPhases);
        SpawnRingTelegraphs(originPos, 14f, 12, sequencedVfx, seqDamage, seqRadius, seqDuration);

        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    private IEnumerator SpawnSpreadConeAttack()
    {
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
                SpawnOneTelegraph(spawnPos, spreadConeVfx, coneDamage, coneRadius, coneDuration);
            }
            yield return new WaitForSeconds(coneRowTelegraphDelay);
        }
        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    private IEnumerator SpawnOneLineAttack()
    {
        Vector3 bossPos = transform.position;
        Vector3 targetPos = playerTarget.position;
        Vector3 forwardDir = (targetPos - bossPos).normalized;
        forwardDir.y = 0;
        Vector3 startPos = bossPos + (forwardDir * 1.5f);
        startPos.y = bossPos.y;

        for (int i = 0; i < lineCount; i++)
        {
            Vector3 spawnPos = startPos + (forwardDir * (i * lineForwardSpacing));
            SpawnOneTelegraph(spawnPos, oneLineVfx, lineDamage, lineRadius, lineDuration);
            yield return new WaitForSeconds(lineTelegraphDelay);
        }
        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    // ==========================================
    // 🟠 2, 3, 4페이즈 코루틴들 (Extra 쿨타임 변수 해제 적용!)
    // ==========================================
    private IEnumerator SpawnChaseCircleAttack()
    {
        for (int i = 0; i < trackingCount; i++)
        {
            Vector3 currentPos = playerTarget.position;
            SpawnOneTelegraph(currentPos, chaseVfx, chaseDamage, chaseRadius, chaseDuration);
            yield return new WaitForSeconds(trackingInterval);
        }
        // [수정] 2페이즈 이상 패턴들은 모두 Extra 변수를 해제합니다!
        isCastingExtraPattern = false;
        currentExtraAttackCooldown = extraAttackCooltime;
    }

    private IEnumerator MultiStingerLaserRoutine()
    {
        Transform vfxFolder = GetVfxContainer();
        for (int i = 0; i < multiLaserCount; i++)
        {
            Vector3 startPoint = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1f;
            Vector3 targetPos = playerTarget.position + Vector3.up * 1f;
            Vector3 lockedDir = (targetPos - startPoint).normalized;

            GameObject activeIndicator = null;
            if (laserIndicatorPrefab != null)
            {
                Vector3 indicatorPos = startPoint + (lockedDir * (multiLaserLength / 2f));
                activeIndicator = Instantiate(laserIndicatorPrefab, indicatorPos, Quaternion.LookRotation(lockedDir), vfxFolder);
                LaserIndicator script = activeIndicator.GetComponent<LaserIndicator>();
                if (script != null) script.Initialize(laserStartRadius, laserEndRadius, laserCoreRadius, multiLaserLength, multiLaserChargeTime);
                Destroy(activeIndicator, multiLaserChargeTime + 0.5f);
            }

            GameObject activeWarningVfx = null;
            if (laserWarningVfx != null)
            {
                activeWarningVfx = Instantiate(laserWarningVfx, startPoint, Quaternion.LookRotation(lockedDir), vfxFolder);
                Destroy(activeWarningVfx, multiLaserChargeTime + 0.5f);
            }

            yield return new WaitForSeconds(multiLaserChargeTime);

            if (activeWarningVfx != null) Destroy(activeWarningVfx);
            if (activeIndicator != null) Destroy(activeIndicator);

            if (laserFireVfx != null)
            {
                GameObject fireVfx = Instantiate(laserFireVfx, startPoint, Quaternion.LookRotation(lockedDir), vfxFolder);
                Destroy(fireVfx, 2f);
            }

            Vector3 boxCenter = startPoint + (lockedDir * (multiLaserLength / 2f));
            Vector3 halfExtents = new Vector3(laserEndRadius, laserEndRadius, multiLaserLength / 2f);
            Collider[] hits = Physics.OverlapBox(boxCenter, halfExtents, Quaternion.LookRotation(lockedDir), playerLayerMask);
            foreach (Collider hit in hits) hit.GetComponent<IDamageable>()?.TakeDamage(DamageInfo.Normal(multiLaserDamage));

            if (i < multiLaserCount - 1) yield return new WaitForSeconds(multiLaserDelay);
        }
        yield return new WaitForSeconds(0.5f);

        isCastingExtraPattern = false;
        currentExtraAttackCooldown = extraAttackCooltime;
    }

    private IEnumerator ShotgunLaserRoutine()
    {
        Vector3 startPoint = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1f;
        Vector3 targetPos = playerTarget.position + Vector3.up * 1.0f;
        Vector3 baseDir = (targetPos - startPoint).normalized;
        Transform vfxFolder = GetVfxContainer();

        List<GameObject> activeIndicators = new List<GameObject>();
        List<Vector3> laserDirections = new List<Vector3>();

        for (int i = 0; i < shotgunPelletCount; i++)
        {
            Vector3 fireDir;
            if (i == 0) fireDir = baseDir;
            else
            {
                float randomDevAngle = Random.Range(0f, spreadAngle / 2f);
                float randomRollAngle = Random.Range(0f, 360f);
                Quaternion deviation = Quaternion.AngleAxis(randomDevAngle, Vector3.up);
                Quaternion roll = Quaternion.AngleAxis(randomRollAngle, Vector3.forward);
                fireDir = Quaternion.LookRotation(baseDir) * roll * deviation * Vector3.forward;
            }
            laserDirections.Add(fireDir);

            if (laserIndicatorPrefab != null)
            {
                Vector3 indicatorPos = startPoint + (fireDir * (shotgunLaserLength / 2f));
                GameObject indicator = Instantiate(laserIndicatorPrefab, indicatorPos, Quaternion.LookRotation(fireDir), vfxFolder);
                LaserIndicator script = indicator.GetComponent<LaserIndicator>();
                if (script != null) script.Initialize(laserStartRadius, laserEndRadius, laserCoreRadius, shotgunLaserLength, shotgunLaserChargeTime);
                activeIndicators.Add(indicator);
                Destroy(indicator, shotgunLaserChargeTime + 0.5f);
            }
        }

        GameObject warningVfx = null;
        if (laserWarningVfx != null)
        {
            warningVfx = Instantiate(laserWarningVfx, startPoint, Quaternion.LookRotation(baseDir), vfxFolder);
            Destroy(warningVfx, shotgunLaserChargeTime + 0.5f);
        }

        yield return new WaitForSeconds(shotgunLaserChargeTime);

        if (warningVfx != null) Destroy(warningVfx);
        foreach (var ind in activeIndicators) if (ind != null) Destroy(ind);

        HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
        for (int i = 0; i < shotgunPelletCount; i++)
        {
            Vector3 dir = laserDirections[i];
            if (laserFireVfx != null)
            {
                GameObject fireVfx = Instantiate(laserFireVfx, startPoint, Quaternion.LookRotation(dir), vfxFolder);
                Destroy(fireVfx, 2f);
            }

            Vector3 boxCenter = startPoint + (dir * (shotgunLaserLength / 2f));
            Vector3 halfExtents = new Vector3(laserEndRadius, laserEndRadius, shotgunLaserLength / 2f);
            Collider[] hits = Physics.OverlapBox(boxCenter, halfExtents, Quaternion.LookRotation(dir), playerLayerMask);
            foreach (Collider hit in hits)
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null && !hitTargets.Contains(damageable))
                {
                    damageable.TakeDamage(DamageInfo.Normal(shotgunLaserDamage));
                    hitTargets.Add(damageable);
                }
            }
        }
        yield return new WaitForSeconds(0.5f);

        isCastingExtraPattern = false;
        currentExtraAttackCooldown = extraAttackCooltime;
    }

    private IEnumerator SlowLargeCircleRoutine()
    {
        Vector3 originPos = playerTarget.position;
        SpawnOneTelegraph(originPos, bigCircleVfx, bigCircleDamage, bigCircleRadius, bigCircleDuration);
        yield return new WaitForSeconds(bigCircleDuration + 0.5f);

        isCastingExtraPattern = false;
        currentExtraAttackCooldown = extraAttackCooltime;
    }

    private IEnumerator SpawnSuicideSpidersRoutine()
    {
        Vector3 centerPos = playerTarget.position;
        List<Vector3> validSpawnPoints = new List<Vector3>();
        Transform vfxContainer = GetVfxContainer();

        for (int i = 0; i < spiderSpawnCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spiderSpawnRadius;
            Vector3 targetPos = centerPos + new Vector3(randomCircle.x, raycastStartHeight, randomCircle.y);

            if (Physics.Raycast(targetPos, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
            {
                validSpawnPoints.Add(hit.point);
                if (spiderSpawnWarningVfx != null)
                {
                    GameObject vfx = Instantiate(spiderSpawnWarningVfx, hit.point, Quaternion.identity, vfxContainer);
                    Destroy(vfx, spiderSpawnDelay + 0.5f);
                }
            }
        }

        yield return new WaitForSeconds(spiderSpawnDelay);

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

        isCastingExtraPattern = false;
        currentExtraAttackCooldown = extraAttackCooltime;
    }

    private IEnumerator TeleportStrikeRoutine()
    {
        if (bossModel == null)
        {
            isCastingExtraPattern = false;
            yield break;
        }
        Transform vfxContainer = GetVfxContainer();

        Vector3 spawnPosition = playerTarget.position;
        Vector3 rayStart = spawnPosition + Vector3.up * raycastStartHeight;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer)) spawnPosition = hit.point;

        SpawnOneTelegraph(spawnPosition, appearVfxPrefab, teleportDamage, teleportRadius, teleportDuration);

        float waitBeforeDisappear = teleportDuration - disappearAnimTime;
        if (waitBeforeDisappear > 0) yield return new WaitForSeconds(waitBeforeDisappear);

        if (disappearVfxPrefab != null) Instantiate(disappearVfxPrefab, transform.position, Quaternion.identity, vfxContainer);
        if (bossCollider != null) bossCollider.enabled = false;

        bossModel.transform.DOScale(Vector3.zero, disappearAnimTime).SetEase(Ease.InBack);
        bossModel.transform.DOLocalMoveY(originalModelLocalPos.y - 3f, disappearAnimTime).SetEase(Ease.InBack);

        yield return new WaitForSeconds(disappearAnimTime);

        transform.position = spawnPosition;
        bossModel.transform.localPosition = new Vector3(originalModelLocalPos.x, originalModelLocalPos.y - 3f, originalModelLocalPos.z);
        bossModel.transform.localScale = Vector3.zero;

        bossModel.transform.DOScale(originalModelScale, appearAnimTime).SetEase(Ease.OutBack);
        bossModel.transform.DOLocalMoveY(originalModelLocalPos.y, appearAnimTime).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(appearAnimTime);

        bossModel.transform.localScale = originalModelScale;
        bossModel.transform.localPosition = originalModelLocalPos;
        if (bossCollider != null) bossCollider.enabled = true;

        yield return new WaitForSeconds(0.5f);

        isCastingExtraPattern = false;
        currentExtraAttackCooldown = extraAttackCooltime;
    }

    // ==========================================
    // ⚙️ 공통 헬퍼 함수
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
        if (container == null) container = new GameObject("Boss_VFX_Container");
        cachedVfxContainer = container.transform;
        return cachedVfxContainer;
    }

    private void SpawnOneTelegraph(Vector3 pos, GameObject vfxPrefab, float damage, float radius, float duration)
    {
        if (telegraphPrefab == null) return;
        GameObject go = Instantiate(telegraphPrefab, pos, Quaternion.identity, GetVfxContainer());
        BossTelegraph script = go.GetComponent<BossTelegraph>();
        if (script != null)
        {
            script.explosionVfxPrefab = vfxPrefab;
            script.Initialize(damage, radius, duration, playerLayerMask);
        }
    }

    private void SpawnRingTelegraphs(Vector3 center, float ringRadius, int count, GameObject vfxPrefab, float damage, float radius, float duration)
    {
        float angleStep = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float angleRad = (i * angleStep) * Mathf.Deg2Rad;
            float spawnX = center.x + (ringRadius * Mathf.Cos(angleRad));
            float spawnZ = center.z + (ringRadius * Mathf.Sin(angleRad));
            Vector3 targetPos = new Vector3(spawnX, center.y, spawnZ);
            SpawnOneTelegraph(targetPos, vfxPrefab, damage, radius, duration);
        }
    }

    public void InitializeStatues(BossStatue[] sceneStatues)
    {
        statues = sceneStatues;
        if (statues != null)
        {
            foreach (var statue in statues) if (statue != null) statue.Setup(this);
        }
    }

    protected override void Die()
    {
        if (bossModel != null)
        {
            bossModel.transform.DOKill();
        }

        if (activeShieldInstance != null)
        {
            Destroy(activeShieldInstance);
        }

        base.Die();
    }
}
