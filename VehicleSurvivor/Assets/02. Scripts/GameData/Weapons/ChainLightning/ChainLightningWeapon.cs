using System.Collections.Generic;
using UnityEngine;

public class ChainLightningWeapon : WeaponBase
{
    [Header("Chain Lightning References")]
    public GameObject lightningEffectPrefab;

    [Header("Target Settings")]
    public LayerMask targetLayer;

    [Header("Legacy Stats")]
    public float damage = 10f;
    public float cooldown = 1f;
    public float detectRadius = 10f;
    public int chainCount = 3;
    public float chainRadius = 5f;

    [Header("Effect Settings")]
    public float lineLifeTime = 0.1f;


    protected override void Update()
    {
        if (HasConfiguredLevelData())
        {
            base.Update();
            return;
        }

        attackTimer += Time.deltaTime;

        if (attackTimer >= GetFinalCooldown(cooldown))
        {
            AttackLegacy();
            attackTimer = 0f;
        }
    }

    protected override void Attack(WeaponLevelData data)
    {
        ChainAttackFromPosition(
            GetDetectRadius(data),
            Mathf.Max(0, data.chainCount),
            data.chainRadius > 0f ? data.chainRadius : GetDetectRadius(data),
            GetFinalDamage(data.Damage)
        );
    }

    private void AttackLegacy()
    {
        ChainAttackFromPosition(
            detectRadius,
            chainCount,
            chainRadius,
            GetFinalDamage(damage)
        );
    }

    private void ChainAttackFromPosition(
        float detectRange,
        int remainingChainCount,
        float searchRadius,
        float finalDamage
    )
    {
        Transform firstTarget = FindClosestTarget(transform.position, detectRange, null);

        if (firstTarget == null)
            return;

        CreateLightningLine(transform.position, firstTarget.position);

        List<Transform> hitTargets = new List<Transform>();

        ChainAttack(
            firstTarget,
            hitTargets,
            remainingChainCount,
            searchRadius,
            finalDamage
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
        if (currentTarget == null || hitTargets.Contains(currentTarget))
            return;

        IDamageable damageable = currentTarget.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(finalDamage);
            Debug.Log($"[ChainLightning] damage: {finalDamage}");
        }

        hitTargets.Add(currentTarget);

        if (remainChain <= 0)
            return;

        Transform nextTarget = FindClosestTarget(currentTarget.position, searchRadius, hitTargets);

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
        Collider[] hits = Physics.OverlapSphere(center, radius, targetLayer);

        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            Transform targetTransform = hit.GetComponentInParent<Transform>();

            if (targetTransform == null)
                continue;

            if (ignoreTargets != null && ignoreTargets.Contains(targetTransform))
                continue;

            float distance = (targetTransform.position - center).sqrMagnitude;

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
        if (lightningEffectPrefab == null)
            return;

        GameObject effect = Instantiate(
            lightningEffectPrefab,
            start,
            Quaternion.identity
        );

        LighteningScript lightning = effect.GetComponentInChildren<LighteningScript>();

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

    private float GetDetectRadius(WeaponLevelData data)
    {
        if (data == null)
            return detectRadius;

        return data.Range > 0f ? data.Range : detectRadius;
    }
}
