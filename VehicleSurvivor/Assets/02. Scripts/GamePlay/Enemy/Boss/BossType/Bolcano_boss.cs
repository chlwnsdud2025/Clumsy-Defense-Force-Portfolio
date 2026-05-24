using System.Collections;
using UnityEngine;

public class Bolcano_Boss : BossEnemy
{
    [Header("Targeting Settings")]
    public LayerMask playerLayerMask;

    [Header("Common Pattern Assets")]
    [Tooltip("경고용으로 바닥에 깔릴 구체 장판 프리팹 (BossTelegraph)")]
    public GameObject telegraphPrefab;

    [Header("Pattern 1: Spreading Ring (원형 연쇄 확장 장판)")]
    public float p1Damage = 30f;
    public float p1Radius = 3f;
    public float p1TelegraphDuration = 1.5f;
    [Tooltip("장판이 중심에서 바깥 단계로 번져나가는 대기 시간")]
    public float timeBetweenPhases = 0.5f;
    [Tooltip("패턴 1 전용 폭발 이펙트")]
    public GameObject p1ExplosionVfx;

    [Header("Pattern 2: Slow & Large Circle (느리고 거대한 단일 장판)")]
    public float p2Damage = 60f;
    [Tooltip("피하기 까다롭도록 아주 크게 설정할 장판의 반지름")]
    public float p2Radius = 8f;
    [Tooltip("플레이어가 범위 밖으로 도망칠 수 있도록 느리게 차오르는 시간")]
    public float p2TelegraphDuration = 3.0f;
    [Tooltip("패턴 2 전용 폭발 이펙트")]
    public GameObject p2ExplosionVfx;

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
            StartCoroutine(SpawnSpreadingRingAttack());
        }
        else
        {
            StartCoroutine(SpawnSlowLargeCircleAttack());
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
    // 패턴 1: 원형 연쇄 확장 장판 (Y축 버그 수정 완료)
    // ==========================================
    private IEnumerator SpawnSpreadingRingAttack()
    {
        Debug.Log($"{gameObject.name}: 패턴 1 (연쇄 장판) 시전!");

        // [수정] 이제 보스의 Y축을 대입하지 않고, 플레이어의 순수 위치(고도 포함)를 그대로 씁니다!
        Vector3 originPos = playerTarget.position;

        SpawnOneTelegraph(originPos, p1Radius, p1TelegraphDuration, p1Damage, p1ExplosionVfx);
        yield return new WaitForSeconds(timeBetweenPhases);

        SpawnRingTelegraphs(originPos, 7f, 6, p1Radius, p1TelegraphDuration, p1Damage, p1ExplosionVfx);
        yield return new WaitForSeconds(timeBetweenPhases);

        //SpawnRingTelegraphs(originPos, 14f, 12, p1Radius, p1TelegraphDuration, p1Damage, p1ExplosionVfx);

        yield return new WaitForSeconds(p1TelegraphDuration + 0.5f);

        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    // ==========================================
    // 패턴 2: 플레이어 발밑에 느리고 거대한 단일 장판 (Y축 버그 수정 완료)
    // ==========================================
    private IEnumerator SpawnSlowLargeCircleAttack()
    {
        Debug.Log($"{gameObject.name}: 패턴 2 (거대 장판) 시전!");

        // [수정] 여기도 마찬가지로 플레이어의 실제 발밑 고도를 100% 반영합니다.
        Vector3 spawnPos = playerTarget.position;

        SpawnOneTelegraph(spawnPos, p2Radius, p2TelegraphDuration, p2Damage, p2ExplosionVfx);

        yield return new WaitForSeconds(p2TelegraphDuration + 0.5f);

        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    private void SpawnOneTelegraph(Vector3 pos, float radius, float duration, float damage, GameObject targetVfx)
    {
        if (telegraphPrefab == null) return;

        GameObject go = Instantiate(telegraphPrefab, pos, Quaternion.identity, GetVfxContainer());
        BossTelegraph script = go.GetComponent<BossTelegraph>();
        if (script != null)
        {
            script.explosionVfxPrefab = targetVfx;
            script.Initialize(damage, radius, duration, playerLayerMask);
        }
    }

    private void SpawnRingTelegraphs(Vector3 center, float ringRadius, int count, float radius, float duration, float damage, GameObject targetVfx)
    {
        float angleStep = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float angleRad = (i * angleStep) * Mathf.Deg2Rad;
            float spawnX = center.x + (ringRadius * Mathf.Cos(angleRad));
            float spawnZ = center.z + (ringRadius * Mathf.Sin(angleRad));

            // 각 가닥의 장판들도 플레이어의 원래 고도(center.y)를 기준으로 생성됩니다.
            SpawnOneTelegraph(new Vector3(spawnX, center.y, spawnZ), radius, duration, damage, targetVfx);
        }
    }
}