using System.Collections;
using UnityEngine;

public class BeeBoss : BossEnemy
{
    [Header("Targeting Settings")]
    public LayerMask playerLayerMask;

    [Header("Pattern 1: Stinger Laser (잔상 단발 레이저)")]
    public float laserDamage = 40f;
    public float laserLength = 25f;
    public float laserChargeTime = 1.5f;
    public Transform firePoint;

    [Header("Laser Telegraph & Core Settings")]
    public GameObject laserIndicatorPrefab;
    public float startRadius = 3.0f;
    public float endRadius = 0.3f;
    public float coreRadius = 0.3f;

    [Header("Laser VFX")]
    public GameObject laserWarningVfx;
    public GameObject laserFireVfx;

    [Header("Pattern 2: Honey Cone Bomb (무빙 원뿔 꿀 폭탄)")]
    public GameObject telegraphPrefab;
    public GameObject honeyExplosionVfx;
    public float bombDamage = 30f;
    public float bombRadius = 3.5f;
    public float bombTelegraphDuration = 1.5f;
    public int coneRows = 3;
    public float rowForwardSpacing = 5f;
    public float circleSideSpacing = 4f;
    public float coneRowTelegraphDelay = 0.3f;

    [Header("Pattern 3: Multi Stinger Laser (잔상 연속 발사)")]
    public int multiLaserCount = 3;
    public float multiLaserChargeTime = 0.5f;
    public float multiLaserDelay = 0.2f;

    protected override void Start()
    {
        base.Start();
    }

    protected override void Attack()
    {
        if (isCastingPattern) return;
        isCastingPattern = true;

        int randomPattern = Random.Range(0, 3);

        if (randomPattern == 0) StartCoroutine(StingerLaserRoutine());
        else if (randomPattern == 1) StartCoroutine(HoneyConeBombRoutine());
        else StartCoroutine(MultiStingerLaserRoutine());
    }

    // ==========================================
    // 패턴 1: 보스는 자유롭게 움직이고, 레이저는 허공에 남겨두는 방식
    // ==========================================
    private IEnumerator StingerLaserRoutine()
    {
        Debug.Log("꿀벌 보스: 레이저 예고선 생성! (보스는 계속 이동)");

        // 1. 발사 위치와 방향을 계산 (이 시점의 위치를 기억해둡니다)
        Vector3 startPoint = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1f;
        Vector3 targetPos = playerTarget.position;
        Vector3 lockedDir = (targetPos - startPoint).normalized;

        // 2. 인디케이터와 이펙트를 보스 자식으로 넣지 않고(부모 없음), 월드에 고정시킵니다.
        GameObject activeIndicator = null;
        if (laserIndicatorPrefab != null)
        {
            Vector3 indicatorPos = startPoint + (lockedDir * (laserLength / 2f));
            activeIndicator = Instantiate(laserIndicatorPrefab, indicatorPos, Quaternion.LookRotation(lockedDir));

            LaserIndicator script = activeIndicator.GetComponent<LaserIndicator>();
            if (script != null) script.Initialize(startRadius, endRadius, coreRadius, laserLength, laserChargeTime);
        }

        GameObject activeWarningVfx = null;
        if (laserWarningVfx != null)
        {
            activeWarningVfx = Instantiate(laserWarningVfx, startPoint, Quaternion.LookRotation(lockedDir));
        }

        // 3. 차징 시간 대기 (이 동안 보스는 자유롭게 돌아다님)
        yield return new WaitForSeconds(laserChargeTime);

        // 4. 발사! (현재 보스 위치가 아니라, 아까 기억해둔 startPoint에서 쏩니다)
        if (activeWarningVfx != null) Destroy(activeWarningVfx);
        if (activeIndicator != null) Destroy(activeIndicator);

        if (laserFireVfx != null) Instantiate(laserFireVfx, startPoint, Quaternion.LookRotation(lockedDir));

        Vector3 boxCenter = startPoint + (lockedDir * (laserLength / 2f));
        Vector3 halfExtents = new Vector3(endRadius, endRadius, laserLength / 2f);

        Collider[] hits = Physics.OverlapBox(boxCenter, halfExtents, Quaternion.LookRotation(lockedDir), playerLayerMask);
        foreach (Collider hit in hits)
        {
            hit.GetComponent<IDamageable>()?.TakeDamage(laserDamage);
        }

        yield return new WaitForSeconds(0.5f);
        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    // ==========================================
    // 패턴 3: 잔상 연속 레이저
    // ==========================================
    private IEnumerator MultiStingerLaserRoutine()
    {
        Debug.Log($"꿀벌 보스: {multiLaserCount}연발 잔상 레이저!");

        for (int i = 0; i < multiLaserCount; i++)
        {
            // 매 발사 시점마다 보스의 '현재' 위치에서 새로운 잔상을 생성
            Vector3 startPoint = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1f;
            Vector3 targetPos = playerTarget.position;
            Vector3 lockedDir = (targetPos - startPoint).normalized;

            GameObject activeIndicator = null;
            if (laserIndicatorPrefab != null)
            {
                Vector3 indicatorPos = startPoint + (lockedDir * (laserLength / 2f));
                activeIndicator = Instantiate(laserIndicatorPrefab, indicatorPos, Quaternion.LookRotation(lockedDir));

                LaserIndicator script = activeIndicator.GetComponent<LaserIndicator>();
                if (script != null) script.Initialize(startRadius, endRadius, coreRadius, laserLength, multiLaserChargeTime);
            }

            GameObject activeWarningVfx = null;
            if (laserWarningVfx != null) activeWarningVfx = Instantiate(laserWarningVfx, startPoint, Quaternion.LookRotation(lockedDir));

            yield return new WaitForSeconds(multiLaserChargeTime);

            if (activeWarningVfx != null) Destroy(activeWarningVfx);
            if (activeIndicator != null) Destroy(activeIndicator);

            if (laserFireVfx != null) Instantiate(laserFireVfx, startPoint, Quaternion.LookRotation(lockedDir));

            Vector3 boxCenter = startPoint + (lockedDir * (laserLength / 2f));
            Vector3 halfExtents = new Vector3(endRadius, endRadius, laserLength / 2f);

            Collider[] hits = Physics.OverlapBox(boxCenter, halfExtents, Quaternion.LookRotation(lockedDir), playerLayerMask);
            foreach (Collider hit in hits)
            {
                hit.GetComponent<IDamageable>()?.TakeDamage(laserDamage);
            }

            if (i < multiLaserCount - 1) yield return new WaitForSeconds(multiLaserDelay);
        }

        yield return new WaitForSeconds(0.5f);
        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    // ==========================================
    // 패턴 2: 멈추지 않는 꿀 폭탄 융단폭격 (동일)
    // ==========================================
    private IEnumerator HoneyConeBombRoutine()
    {
        for (int row = 0; row < coneRows; row++)
        {
            Vector3 bossPos = transform.position;
            Vector3 targetPos = playerTarget.position;
            Vector3 forwardDir = (targetPos - bossPos).normalized;
            forwardDir.y = 0;

            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
            Vector3 coneApexPos = bossPos + (forwardDir * 1.5f);
            coneApexPos.y = bossPos.y;

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

        yield return new WaitForSeconds(bombTelegraphDuration);
        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    private void SpawnOneTelegraph(Vector3 pos)
    {
        if (telegraphPrefab == null) return;

        GameObject go = Instantiate(telegraphPrefab, pos, Quaternion.identity);
        BossTelegraph script = go.GetComponent<BossTelegraph>();

        if (script != null)
        {
            script.explosionVfxPrefab = honeyExplosionVfx;
            script.Initialize(bombDamage, bombRadius, bombTelegraphDuration, playerLayerMask);
        }
    }
}