using System.Collections.Generic;
using UnityEngine;

public class ChainLightningWeapon : WeaponBase
{
    [Header("체인 번개 설정")]
    public LayerMask enemyLayer;
    public float detectRadius = 10f;

    [Header("이펙트 설정")]
    public GameObject lightningEffectPrefab;
    public float lineLifeTime = 0.1f;

    protected override void Attack(WeaponLevelData data)
    {
        Enemy firstTarget = FindClosestEnemy();

        if (firstTarget == null)
            return;

        CreateLightningLine(
            transform.position,
            firstTarget.transform.position
        );

        List<Enemy> hitEnemies = new List<Enemy>();

        ChainAttack(
            firstTarget,
            hitEnemies,
            data.chainCount,
            data.chainRadius,
            GetFinalDamage(data.damage)
        );
    }

    private void ChainAttack(
        Enemy currentEnemy,
        List<Enemy> hitEnemies,
        int remainChain,
        float chainRadius,
        float damage
    )
    {
        if (currentEnemy == null)
            return;

        if (hitEnemies.Contains(currentEnemy))
            return;

        currentEnemy.TakeDamage(damage);
        hitEnemies.Add(currentEnemy);

        Debug.Log($"[ChainLightning] {currentEnemy.name} 피격 / 데미지: {damage}");

        if (remainChain <= 0)
            return;

        Enemy nextEnemy = FindNextEnemy(
            currentEnemy.transform.position,
            chainRadius,
            hitEnemies
        );

        if (nextEnemy == null)
            return;

        CreateLightningLine(
            currentEnemy.transform.position,
            nextEnemy.transform.position
        );

        ChainAttack(
            nextEnemy,
            hitEnemies,
            remainChain - 1,
            chainRadius,
            damage
        );
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

        LighteningScript lightning =
            effect.GetComponentInChildren<LighteningScript>();

        if (lightning != null)
        {
            lightning.SetTarget(start, end);
            lightning.StartLightning();
        }

        Destroy(effect, lineLifeTime);
    }

    private Enemy FindClosestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectRadius,
            enemyLayer
        );

        Enemy closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
                continue;

            Enemy enemy = hit.GetComponentInParent<Enemy>();

            if (enemy == null)
                continue;

            float distance =
                (enemy.transform.position - transform.position).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }

    private Enemy FindNextEnemy(
        Vector3 center,
        float radius,
        List<Enemy> hitEnemies
    )
    {
        Collider[] hits = Physics.OverlapSphere(
            center,
            radius,
            enemyLayer
        );

        Enemy closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
                continue;

            Enemy enemy = hit.GetComponentInParent<Enemy>();

            if (enemy == null)
                continue;

            if (hitEnemies.Contains(enemy))
                continue;

            float distance =
                (enemy.transform.position - center).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}