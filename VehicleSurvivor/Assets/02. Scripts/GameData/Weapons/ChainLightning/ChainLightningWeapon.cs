using System.Collections.Generic;
using UnityEngine;

public class ChainLightningWeapon : WeaponBase
{
    [Header("체인 번개 데이터")]
    public ChainLightningWeaponDataSO chainData;

    [Header("레이어 설정")]
    public LayerMask targetLayer;

    [Header("임시 테스트 스탯")]
    public float damage = 10f;
    public float cooldown = 1f;
    public float detectRadius = 10f;
    public int chainCount = 3;
    public float chainRadius = 5f;

    [Header("이펙트 설정")]
    public float lineLifeTime = 0.1f;

    private float attackTimer;

    private void Update()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer >= GetFinalCooldown(cooldown))
        {
            Attack();
            attackTimer = 0f;
        }
    }

    private void Attack()
    {
        Transform firstTarget = FindClosestTarget(transform.position, detectRadius, null);

        if (firstTarget == null)
            return;

        CreateLightningLine(transform.position, firstTarget.position);

        List<Transform> hitTargets = new List<Transform>();

        ChainAttack(
            firstTarget,
            hitTargets,
            chainCount,
            chainRadius,
            GetFinalDamage(damage)
        );
    }

    private void ChainAttack(
        Transform currentTarget,
        List<Transform> hitTargets,
        int remainChain,
        float searchRadius,
        float finalDamage
    )
    {
        if (currentTarget == null)
            return;

        if (hitTargets.Contains(currentTarget))
            return;

        IDamageable damageable =
            currentTarget.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(finalDamage);
            Debug.Log($"[ChainLightning] 데미지: {finalDamage}");
        }

        hitTargets.Add(currentTarget);

        if (remainChain <= 0)
            return;

        Transform nextTarget =
            FindClosestTarget(currentTarget.position, searchRadius, hitTargets);

        if (nextTarget == null)
            return;

        CreateLightningLine(currentTarget.position, nextTarget.position);

        ChainAttack(
            nextTarget,
            hitTargets,
            remainChain - 1,
            searchRadius,
            finalDamage
        );
    }

    private Transform FindClosestTarget(
        Vector3 center,
        float radius,
        List<Transform> ignoreTargets
    )
    {
        Collider[] hits = Physics.OverlapSphere(
            center,
            radius,
            targetLayer
        );

        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            Transform targetTransform =
                hit.GetComponentInParent<Transform>();

            if (targetTransform == null)
                continue;

            if (ignoreTargets != null &&
                ignoreTargets.Contains(targetTransform))
                continue;

            float distance =
                (targetTransform.position - center).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = targetTransform;
            }
        }

        return closestTarget;
    }

    private void CreateLightningLine(Vector3 start, Vector3 end)
    {
        if (chainData == null)
            return;

        if (chainData.lightningEffectPrefab == null)
            return;

        GameObject effect = Instantiate(
            chainData.lightningEffectPrefab,
            start,
            Quaternion.identity
        );

        LighteningScript lightning =
            effect.GetComponentInChildren<LighteningScript>();

        if (lightning != null)
        {
            lightning.SetTarget(start, end);
            lightning.StartLightning();
        }

        Destroy(effect, lineLifeTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}