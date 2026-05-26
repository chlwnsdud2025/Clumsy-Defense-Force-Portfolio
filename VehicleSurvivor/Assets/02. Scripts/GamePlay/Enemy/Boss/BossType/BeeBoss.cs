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
    private Transform cachedVfxContainer = null;

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
    // 패턴 1: 보스는 자유롭게 움직이고, 레이저는 허공에 남겨두는 방식
    // ==========================================
    private IEnumerator StingerLaserRoutine()
    {
        Debug.Log("꿀벌 보스: 레이저 예고선 생성! (보스는 계속 이동)");

        Vector3 startPoint = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1f;
        Vector3 targetPos = playerTarget.position;
        Vector3 lockedDir = (targetPos - startPoint).normalized;

        // [핵심 수정] 무조건 전용 폴더(GetVfxContainer) 안으로 집어넣습니다!
        Transform vfxFolder = GetVfxContainer();

        GameObject activeIndicator = null;
        if (laserIndicatorPrefab != null)
        {
            Vector3 indicatorPos = startPoint + (lockedDir * (laserLength / 2f));
            activeIndicator = Instantiate(laserIndicatorPrefab, indicatorPos, Quaternion.LookRotation(lockedDir), vfxFolder);

            LaserIndicator script = activeIndicator.GetComponent<LaserIndicator>();
            if (script != null) script.Initialize(startRadius, endRadius, coreRadius, laserLength, laserChargeTime);

            // [안전장치] 보스가 죽어서 코루틴이 끊겨도, 지정된 시간 뒤에 무조건 하이어라키에서 삭제됩니다!
            Destroy(activeIndicator, laserChargeTime + 0.5f);
        }

        GameObject activeWarningVfx = null;
        if (laserWarningVfx != null)
        {
            activeWarningVfx = Instantiate(laserWarningVfx, startPoint, Quaternion.LookRotation(lockedDir), vfxFolder);
            Destroy(activeWarningVfx, laserChargeTime + 0.5f); // 안전장치
        }

        // 3. 차징 시간 대기
        yield return new WaitForSeconds(laserChargeTime);

        // 4. 발사 직전 예고선 강제 삭제
        if (activeWarningVfx != null) Destroy(activeWarningVfx);
        if (activeIndicator != null) Destroy(activeIndicator);

        // 5. 발사 이펙트 생성 및 폴더 삽입
        if (laserFireVfx != null)
        {
            GameObject fireVfx = Instantiate(laserFireVfx, startPoint, Quaternion.LookRotation(lockedDir), vfxFolder);
            Destroy(fireVfx, 2f); // 2초 뒤 자동 삭제
        }

        // 6. 데미지 판정
        Vector3 boxCenter = startPoint + (lockedDir * (laserLength / 2f));
        Vector3 halfExtents = new Vector3(endRadius, endRadius, laserLength / 2f);

        Collider[] hits = Physics.OverlapBox(boxCenter, halfExtents, Quaternion.LookRotation(lockedDir), playerLayerMask);
        foreach (Collider hit in hits)
        {
            hit.GetComponent<IDamageable>()?.TakeDamage(DamageInfo.Normal(laserDamage));
        }

        yield return new WaitForSeconds(0.5f);
        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    // 패턴 3: 잔상 연속 레이저
  
    private IEnumerator MultiStingerLaserRoutine()
    {
        Debug.Log($"꿀벌 보스: {multiLaserCount}연발 잔상 레이저!");

        Transform vfxFolder = GetVfxContainer();

        for (int i = 0; i < multiLaserCount; i++)
        {
            Vector3 startPoint = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1f;
            Vector3 targetPos = playerTarget.position;
            Vector3 lockedDir = (targetPos - startPoint).normalized;

            GameObject activeIndicator = null;
            if (laserIndicatorPrefab != null)
            {
                Vector3 indicatorPos = startPoint + (lockedDir * (laserLength / 2f));
                activeIndicator = Instantiate(laserIndicatorPrefab, indicatorPos, Quaternion.LookRotation(lockedDir), vfxFolder);

                LaserIndicator script = activeIndicator.GetComponent<LaserIndicator>();
                if (script != null) script.Initialize(startRadius, endRadius, coreRadius, laserLength, multiLaserChargeTime);

                Destroy(activeIndicator, multiLaserChargeTime + 0.5f); // 안전장치
            }

            GameObject activeWarningVfx = null;
            if (laserWarningVfx != null)
            {
                activeWarningVfx = Instantiate(laserWarningVfx, startPoint, Quaternion.LookRotation(lockedDir), vfxFolder);
                Destroy(activeWarningVfx, multiLaserChargeTime + 0.5f); // 안전장치
            }

            yield return new WaitForSeconds(multiLaserChargeTime);

            if (activeWarningVfx != null) Destroy(activeWarningVfx);
            if (activeIndicator != null) Destroy(activeIndicator);

            if (laserFireVfx != null)
            {
                GameObject fireVfx = Instantiate(laserFireVfx, startPoint, Quaternion.LookRotation(lockedDir), vfxFolder);
                Destroy(fireVfx, 2f);
            }

            Vector3 boxCenter = startPoint + (lockedDir * (laserLength / 2f));
            Vector3 halfExtents = new Vector3(endRadius, endRadius, laserLength / 2f);

            Collider[] hits = Physics.OverlapBox(boxCenter, halfExtents, Quaternion.LookRotation(lockedDir), playerLayerMask);
            foreach (Collider hit in hits)
            {
                hit.GetComponent<IDamageable>()?.TakeDamage(DamageInfo.Normal(laserDamage));
            }

            if (i < multiLaserCount - 1) yield return new WaitForSeconds(multiLaserDelay);
        }

        yield return new WaitForSeconds(0.5f);
        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    // ==========================================
    // 패턴 2: 멈추지 않는 꿀 폭탄 융단폭격
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

        // [핵심 수정] 하이어라키 최상단에 도배되던 폭탄 장판들까지 완벽하게 전용 폴더에 가둬버립니다!
        GameObject go = Instantiate(telegraphPrefab, pos, Quaternion.identity, GetVfxContainer());
        BossTelegraph script = go.GetComponent<BossTelegraph>();

        if (script != null)
        {
            script.explosionVfxPrefab = honeyExplosionVfx;
            script.Initialize(bombDamage, bombRadius, bombTelegraphDuration, playerLayerMask);
        }
    }
}