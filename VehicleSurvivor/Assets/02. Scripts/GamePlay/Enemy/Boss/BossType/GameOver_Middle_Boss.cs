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

    // 부모의 abstract 함수를 강제로 덮어써서 자신만의 패턴을 실행합니다.
    protected override void Attack()
    {
        if (isCastingPattern) return;
        isCastingPattern = true;

        StartCoroutine(SpawnOneLineAttack());
               
        
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

        // [수정] 추적 패턴 변수 재사용을 중단하고 독립된 lineCount 변수 사용
        for (int i = 0; i < lineCount; i++)
        {
            // [수정] lineForwardSpacing 변수 사용
            Vector3 spawnPos = startPos + (forwardDir * (i * lineForwardSpacing));

            SpawnOneTelegraph(spawnPos);

            // [수정] lineTelegraphDelay 변수 사용
            yield return new WaitForSeconds(lineTelegraphDelay);
        }

        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    // 공통 헬퍼 함수 1: 구체 장판 하나 소환
    private void SpawnOneTelegraph(Vector3 pos)
    {
        if (telegraphPrefab == null) return;

        GameObject go = Instantiate(telegraphPrefab, pos, Quaternion.identity);
        BossTelegraph script = go.GetComponent<BossTelegraph>();
        if (script != null) script.Initialize(baseDamage, baseRadius, telegraphDuration, playerLayerMask);
    }

    
}
