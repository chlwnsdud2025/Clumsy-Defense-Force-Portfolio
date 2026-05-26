using System.Collections.Generic;
using UnityEngine;

public class ChainLightningWeapon : WeaponBase
{
    [Header("Lightning References")]
    public GameObject lightningEffectPrefab;

    [Header("Effect Option")]
    public float effectLifeTime = 0.15f;

    protected override void Attack(WeaponLevelData data)
    {
        if (data == null)
            return;

        float detectRadius = GetFinalWeaponScale(data.Radius);
        float chainRadius = GetFinalWeaponScale(data.ChainRadius);
        DamageInfo damageInfo = GetFinalDamage(data.Damage);

        Transform firstTarget = FindClosestTarget(
            transform.position,
            detectRadius,
            null
        );

        if (firstTarget == null)
        {
            Debug.Log($"[ChainLightning] 반경 {detectRadius} 내 대상 없음");
            return;
        }

        List<Transform> hitTargets = new List<Transform>();

        ChainAttack(
            transform.position,
            firstTarget,
            hitTargets,
            data.ChainCount,
            chainRadius,
            damageInfo
        );
    }

    private void ChainAttack(
        Vector3 startPosition,
        Transform currentTarget,
        List<Transform> hitTargets,
        int remainChainCount,
        float chainRadius,
        DamageInfo damageInfo
    )
    {
        if (currentTarget == null)
            return;

        if (hitTargets.Contains(currentTarget))
            return;

        IDamageable damageable =
            currentTarget.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        CreateLightningEffect(
            startPosition,
            currentTarget.position
        );

        damageable.TakeDamage(damageInfo);

        Debug.Log(
            $"[ChainLightning] {currentTarget.name} 에게 {damageInfo.Amount} 데미지"
        );

        hitTargets.Add(currentTarget);

        if (remainChainCount <= 0)
            return;

        Transform nextTarget = FindClosestTarget(
            currentTarget.position,
            chainRadius,
            hitTargets
        );

        if (nextTarget == null)
            return;

        ChainAttack(
            currentTarget.position,
            nextTarget,
            hitTargets,
            remainChainCount - 1,
            chainRadius,
            damageInfo
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
            radius
        );

        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (IsPlayerLayer(hit))
                continue;

            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            Transform target =
                damageable is MonoBehaviour mono
                    ? mono.transform
                    : hit.transform;

            if (ignoreTargets != null &&
                ignoreTargets.Contains(target))
                continue;

            float distance =
                (target.position - center).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
            }
        }

        return closestTarget;
    }

    private void CreateLightningEffect(
        Vector3 start,
        Vector3 end
    )
    {
        if (lightningEffectPrefab == null)
            return;

        GameObject effect = Instantiate(
            lightningEffectPrefab,
            start,
            lightningEffectPrefab.transform.rotation
        );

        LighteningScript lightning =
            effect.GetComponentInChildren<LighteningScript>();

        if (lightning != null)
        {
            lightning.SetTarget(start, end);
            lightning.StartLightning();
        }

        Destroy(effect, effectLifeTime);
    }

    private bool IsPlayerLayer(Collider other)
    {
        int playerLayer = LayerMask.NameToLayer("Player");

        if (playerLayer == -1)
            return false;

        if (other.gameObject.layer == playerLayer)
            return true;

        Transform root = other.transform.root;

        return root.gameObject.layer == playerLayer;
    }

    private void OnDrawGizmosSelected()
    {
        WeaponLevelData data = GetCurrentLevelData();

        if (data == null)
            return;

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            GetFinalWeaponScale(data.Radius)
        );
    }
}