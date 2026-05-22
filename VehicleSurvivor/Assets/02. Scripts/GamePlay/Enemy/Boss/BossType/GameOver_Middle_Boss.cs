using UnityEngine;
using System.Collections;

public class GameOver_Middle_Boss : BossEnemy
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

    [Header("Pattern 4: One Line Pattern (일렬 순차 장판)")]
    public int lineCount = 6;                // 일렬로 몇 개의 장판을 깔 것인가
    public float lineForwardSpacing = 4f;    // 장판 사이의 앞방향 간격
    public float lineTelegraphDelay = 0.2f;  // 장판이 연달아 나타나는 시간차

    [Header("VFX")]
    public GameObject explosionEffectPrefab;

    private Transform cachedVfxContainer = null;

    // 부모의 abstract 함수를 강제로 덮어써서 자신만의 패턴을 실행합니다.
    protected override void Attack()
    {
        if (isCastingPattern) return;
        isCastingPattern = true;

        StartCoroutine(SpawnOneLineAttack());
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
            script.explosionVfxPrefab = explosionEffectPrefab;

            script.Initialize(baseDamage, baseRadius, telegraphDuration, playerLayerMask);
        }
    }
}