using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;


public class Rock_Golem_Middle_Boss : BossEnemy
{
    [Header("Tracker Boss Settings")]
    public LayerMask playerLayerMask;
    public GameObject telegraphPrefab;

    [Header("Attack Stats")]
    public float baseDamage = 20f;
    public float baseRadius = 3f;
    public float telegraphDuration = 1.0f;

    [Header("Chase Pattern Settings")]
    public int trackingCount = 3;         // 쫓아가는 횟수 (중간보스라 횟수를 줄임)
    public float trackingInterval = 1.0f; // 생성 간격

    // 부모(BossEnemy)의 공격 함수를 덮어씁니다. 랜덤 돌릴 필요 없이 무조건 추적 장판만 실행!
    protected override void Attack()
    {
        if (isCastingPattern) return;
        isCastingPattern = true;

        StartCoroutine(SpawnChaseCircleAttack());
    }

    private IEnumerator SpawnChaseCircleAttack()
    {
        Debug.Log($"TrackerMidBoss: 추적 장판 {trackingCount}연발 시전!");

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

    private void SpawnOneTelegraph(Vector3 pos)
    {
        if (telegraphPrefab == null) return;

        GameObject go = Instantiate(telegraphPrefab, pos, Quaternion.identity);
        BossTelegraph script = go.GetComponent<BossTelegraph>();
        if (script != null) script.Initialize(baseDamage, baseRadius, telegraphDuration, playerLayerMask);
    }
}
