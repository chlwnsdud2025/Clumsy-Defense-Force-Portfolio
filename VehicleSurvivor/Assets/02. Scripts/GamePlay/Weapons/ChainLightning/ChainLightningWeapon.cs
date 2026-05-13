using System.Collections.Generic;
using UnityEngine;

public class ChainLightningWeapon : WeaponBase
{
    [Header("체인 번개 설정")]
    public LayerMask enemyLayer;

    [Header("시작 탐색 범위")]
    public float detectRadius = 10f;

    protected override void Attack(WeaponLevelData data)
    {
        Enemy firstTarget = FindClosestEnemy();

        if (firstTarget == null)
            return;

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

        if (remainChain <= 0)
            return;

        Enemy nextEnemy = FindNextEnemy(
            currentEnemy.transform.position,
            chainRadius,
            hitEnemies
        );

        if (nextEnemy != null)
        {
            ChainAttack(
                nextEnemy,
                hitEnemies,
                remainChain - 1,
                chainRadius,
                damage
            );
        }
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