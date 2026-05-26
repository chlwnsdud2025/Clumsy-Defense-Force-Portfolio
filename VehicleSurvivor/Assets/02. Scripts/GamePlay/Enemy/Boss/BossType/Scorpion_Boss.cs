using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScorpionBoss : BossEnemy
{
    [Header("Targeting Settings")]
    public LayerMask playerLayerMask;

    [Header("Pattern 1: Shotgun Laser (원형 산탄 레이저)")]
    public Transform tailFirePoint;
    public int pelletCount = 15;
    public float spreadAngle = 40f;
    public float laserDamage = 20f;
    public float laserLength = 30f;
    public float laserChargeTime = 1.2f;
    public GameObject laserIndicatorPrefab;
    public GameObject laserWarningVfx;
    public GameObject laserFireVfx;
    public float startRadius = 3.0f;
    public float endRadius = 0.3f;
    public float coreRadius = 0.3f;

    // 패턴 2: 4방향 추격 회오리 (Chasing Tornado)
    [Header("Pattern 2: Chasing Tornado (4방향 추격 회오리)")]
    [Tooltip("경고용으로 바닥에 깔릴 구체 장판 프리팹 (BossTelegraph)")]
    public GameObject telegraphPrefab;
    [Tooltip("실제로 플레이어를 쫓아갈 회오리 이펙트 프리팹")]
    public GameObject tornadoPrefab;

    public float tornadoDamagePerTick = 10f; // 한 번 긁힐 때 데미지
    public float tornadoSpeed = 4.5f;        // 쫓아가는 속도
    public float tornadoDuration = 6.0f;     // 회오리가 살아있는 시간
    public float tornadoRadius = 2.5f;       // 장판 및 회오리의 크기
    public float tornadoChargeTime = 1.5f;   // 장판 생성 후 회오리가 튀어나올 때까지의 시간
    public float tornadoTickInterval = 0.3f; // 데미지가 들어가는 간격 (0.3초마다 타격)

    [Tooltip("플레이어 위치 기준 동서남북으로 얼마나 떨어져서 소환될지 설정하는 거리")]
    public float tornadoSpawnOffset = 5.0f;  // 소환 거리 오프셋

    private Transform cachedVfxContainer = null;

    protected override void Start()
    {
        base.Start();
    }

    protected override void Attack()
    {
        if (isCastingPattern) return;
        isCastingPattern = true;

        int randomPattern = Random.Range(0, 2);

        if (randomPattern == 0)
        {
            StartCoroutine(ShotgunLaserRoutine());
        }
        else
        {
            StartCoroutine(ChasingTornadoRoutine());
        }
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
    // [개조 완료] 패턴 2: 4방향 추격 회오리 코루틴
    // ==========================================
    private IEnumerator ChasingTornadoRoutine()
    {
        Debug.Log("전갈 보스: 플레이어 주변 동서남북 4방향에 모래 회오리 폭포 소환!");

        // 플레이어의 현재 기준 위치 저장
        Vector3 centerPos = playerTarget.position;
        centerPos.y = transform.position.y;

        // 동서남북을 가리키는 방향 벡터 배열 (앞, 뒤, 오른쪽, 왼쪽)
        Vector3[] directions = new Vector3[]
        {
            Vector3.forward,  // 북
            Vector3.back,     // 남
            Vector3.right,    // 동
            Vector3.left      // 서
        };

        // 실제 소환될 4개의 좌표를 저장해둘 리스트
        List<Vector3> spawnPositions = new List<Vector3>();

        // 1. 4방향 위치를 계산하고 경고 장판(Telegraph)을 동시에 생성합니다.
        foreach (Vector3 dir in directions)
        {
            // 플레이어 중심 + (방향 * 거리)
            Vector3 spawnPos = centerPos + (dir * tornadoSpawnOffset);
            spawnPositions.Add(spawnPos);

            if (telegraphPrefab != null)
            {
                GameObject indicator = Instantiate(telegraphPrefab, spawnPos, Quaternion.identity, GetVfxContainer());
                BossTelegraph script = indicator.GetComponent<BossTelegraph>();

                if (script != null)
                {
                    script.explosionVfxPrefab = null; // 자체 폭발 이펙트는 비활성화
                    script.Initialize(0f, tornadoRadius, tornadoChargeTime, playerLayerMask);
                }
            }
        }

        // 2. 장판이 차오르는 대기 시간 (플레이어가 탈출로를 찾을 시간)
        yield return new WaitForSeconds(tornadoChargeTime);

        // 3. 장판이 터지는 순간, 계산해둔 4개의 위치에 회오리 일제히 소환!
        if (tornadoPrefab != null)
        {
            foreach (Vector3 spawnPos in spawnPositions)
            {
                GameObject tornado = Instantiate(tornadoPrefab, spawnPos, Quaternion.identity, GetVfxContainer());
                ChasingTornado tornadoScript = tornado.GetComponent<ChasingTornado>();

                if (tornadoScript == null) tornadoScript = tornado.AddComponent<ChasingTornado>();

                // 4개의 회오리 각각이 모두 실시간으로 플레이어를 타겟팅하여 추격 지형 이동을 개시합니다.
                tornadoScript.Initialize(playerTarget, tornadoSpeed, tornadoDamagePerTick, tornadoRadius, tornadoDuration, tornadoTickInterval, playerLayerMask);
            }
        }

        yield return new WaitForSeconds(0.5f);
        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    // (ShotgunLaserRoutine은 이전과 완벽히 동일하여 생략 없이 안전하게 내장 유지)
    private IEnumerator ShotgunLaserRoutine()
    {
        Debug.Log($"전갈 보스: {pelletCount}가닥 리얼 샷건 레이저 차징!");

        Vector3 startPoint = tailFirePoint != null ? tailFirePoint.position : transform.position + Vector3.up * 1f;
        Vector3 targetPos = playerTarget.position + Vector3.up * 1.0f;
        Vector3 baseDir = (targetPos - startPoint).normalized;

        Transform vfxFolder = GetVfxContainer();

        List<GameObject> activeIndicators = new List<GameObject>();
        List<Vector3> laserDirections = new List<Vector3>();

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 fireDir;
            if (i == 0)
            {
                fireDir = baseDir;
            }
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
                Vector3 indicatorPos = startPoint + (fireDir * (laserLength / 2f));
                GameObject indicator = Instantiate(laserIndicatorPrefab, indicatorPos, Quaternion.LookRotation(fireDir), vfxFolder);

                LaserIndicator script = indicator.GetComponent<LaserIndicator>();
                if (script != null) script.Initialize(startRadius, endRadius, coreRadius, laserLength, laserChargeTime);

                activeIndicators.Add(indicator);
                Destroy(indicator, laserChargeTime + 0.5f);
            }
        }

        GameObject warningVfx = null;
        if (laserWarningVfx != null)
        {
            warningVfx = Instantiate(laserWarningVfx, startPoint, Quaternion.LookRotation(baseDir), vfxFolder);
            Destroy(warningVfx, laserChargeTime + 0.5f);
        }

        yield return new WaitForSeconds(laserChargeTime);

        if (warningVfx != null) Destroy(warningVfx);
        foreach (var ind in activeIndicators)
        {
            if (ind != null) Destroy(ind);
        }

        HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 dir = laserDirections[i];

            if (laserFireVfx != null)
            {
                GameObject fireVfx = Instantiate(laserFireVfx, startPoint, Quaternion.LookRotation(dir), vfxFolder);
                Destroy(fireVfx, 2f);
            }

            Vector3 boxCenter = startPoint + (dir * (laserLength / 2f));
            Vector3 halfExtents = new Vector3(endRadius, endRadius, laserLength / 2f);

            Collider[] hits = Physics.OverlapBox(boxCenter, halfExtents, Quaternion.LookRotation(dir), playerLayerMask);
            foreach (Collider hit in hits)
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null && !hitTargets.Contains(damageable))
                {
                    damageable.TakeDamage(laserDamage);
                    hitTargets.Add(damageable);
                }
            }
        }

        yield return new WaitForSeconds(0.5f);
        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }
}