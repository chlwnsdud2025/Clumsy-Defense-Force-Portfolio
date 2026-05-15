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

    [Header("VFX")]
    public GameObject explosionEffectPrefab;


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
        if (script != null)
        {
            // [핵심 추가] 보스가 설정한 폭발 이펙트를 장판에게 쥐여줍니다!
            script.explosionVfxPrefab = explosionEffectPrefab;

            script.Initialize(baseDamage, baseRadius, telegraphDuration, playerLayerMask);
        }
    }
}
