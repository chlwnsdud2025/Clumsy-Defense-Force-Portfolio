using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TwinBoss_Queen : BossEnemy
{
    [Header("Pattern: Chain Telegraph (연속 장판 추격)")]
    [Tooltip("장판(예고선) 프리팹을 연결하세요.")]
    public GameObject telegraphPrefab;

    [Tooltip("장판이 터질 때 나올 폭발 이펙트 프리팹을 연결하세요. (★추가됨)")]
    public GameObject explosionEffectPrefab;

    [Tooltip("연속으로 깔릴 장판의 총 개수")]
    public int chainCount = 4;

    [Tooltip("장판의 반지름 크기")]
    public float telegraphRadius = 2.5f;

    [Tooltip("각 장판이 터지기 전까지의 예고 시간(초)")]
    public float telegraphDuration = 1.0f;

    [Tooltip("장판과 다음 장판 생성 사이의 시간 간격(초)")]
    public float delayBetweenChain = 0.4f;

    [Tooltip("이 패턴에 맞았을 때 들어가는 데미지")]
    public float chainDamage = 30f;

    private Transform cachedVfxContainer = null;

    protected override void Start()
    {
        base.Start();
    }

    protected override void Attack()
    {
        if (isCastingPattern) return;
        isCastingPattern = true;

        StartCoroutine(SpawnChainTelegraphRoutine());
    }

    private IEnumerator SpawnChainTelegraphRoutine()
    {
        if (playerTarget == null)
        {
            isCastingPattern = false;
            yield break;
        }

        Debug.Log("쌍둥이 퀸: 플레이어 추격 연속 장판 시전!");
        Transform vfxContainer = GetVfxContainer();

        for (int i = 0; i < chainCount; i++)
        {
            if (playerTarget == null) break;

            Vector3 targetPosition = playerTarget.position;
            Vector3 spawnPosition = targetPosition;
            Vector3 rayStart = targetPosition + Vector3.up * raycastStartHeight;

            // 지형 추적 보정
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
            {
                spawnPosition = hit.point;
            }

            if (telegraphPrefab != null)
            {
                GameObject telegraphInstance = Instantiate(telegraphPrefab, spawnPosition, Quaternion.identity, vfxContainer);
                BossTelegraph script = telegraphInstance.GetComponent<BossTelegraph>();

                if (script != null)
                {
                    script.explosionVfxPrefab = explosionEffectPrefab;

                    script.Initialize(chainDamage, telegraphRadius, telegraphDuration, groundLayer);
                }
            }

            yield return new WaitForSeconds(delayBetweenChain);
        }

        yield return new WaitForSeconds(1.0f);
        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    private Transform GetVfxContainer()
    {
        if (cachedVfxContainer != null) return cachedVfxContainer;

        if (EnemyManager.Instance != null && EnemyManager.Instance.FxParent != null)
        {
            cachedVfxContainer = EnemyManager.Instance.FxParent;
        }
        return cachedVfxContainer;
    }

    protected override void HandleStageClear()
    {
        TwinBossManager manager = GetComponentInParent<TwinBossManager>();
        if (manager != null)
        {
            manager.OnTwinDied();
        }
    }
}