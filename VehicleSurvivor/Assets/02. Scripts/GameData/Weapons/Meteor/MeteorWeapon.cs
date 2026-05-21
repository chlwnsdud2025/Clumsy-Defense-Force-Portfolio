using UnityEngine;

public class MeteorWeapon : WeaponBase
{
    [Header("Meteor References")]
    public GameObject meteorPrefab;

    [Header("Target Settings")]
    public LayerMask enemyLayer;

    [Header("Spawn Settings")]
    public float spawnRadius = 15f;
    public float spawnHeight = 12f;
    public float groundY = 0f;

    [Header("Safe Distance")]
    public float safeRadius = 7f;
    public int maxTryCount = 20;

    [Header("Fall Direction")]
    public float diagonalOffset = 12f;
    public float randomAngleRange = 20f;

    protected override void Attack(WeaponLevelData data)
    {
        SpawnMeteor(
            GetFinalDamage(data.Damage),
            GetExplosionRadius(data),
            GetMeteorDelay(data)
        );
    }

    private void SpawnMeteor(
        float finalDamage,
        float finalExplosionRadius,
        float finalMeteorDelay
    )
    {
        if (meteorPrefab == null)
            return;

        Vector3 targetPosition = GetRandomTargetPosition();
        Vector3 spawnPosition = GetDiagonalSpawnPosition(targetPosition);

        GameObject meteor = Instantiate(
            meteorPrefab,
            spawnPosition,
            Quaternion.identity
        );

        MeteorProjectile projectile = meteor.GetComponent<MeteorProjectile>();

        if (projectile != null)
        {
            projectile.Init(
                finalDamage,
                finalExplosionRadius,
                finalMeteorDelay,
                enemyLayer,
                targetPosition
            );
        }
    }

    private Vector3 GetRandomTargetPosition()
    {
        Vector3 targetPosition = transform.position;

        for (int i = 0; i < maxTryCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;

            Vector3 randomPosition =
                transform.position +
                new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (Vector3.Distance(transform.position, randomPosition) >= safeRadius)
            {
                targetPosition = randomPosition;
                break;
            }
        }

        targetPosition.y = groundY;
        return targetPosition;
    }

    private Vector3 GetDiagonalSpawnPosition(Vector3 targetPosition)
    {
        Vector3 baseDirection = new Vector3(-1f, 0f, 1f).normalized;
        float randomAngle = Random.Range(-randomAngleRange, randomAngleRange);
        Vector3 finalDirection = Quaternion.Euler(0f, randomAngle, 0f) * baseDirection;

        return targetPosition
               + finalDirection * diagonalOffset
               + Vector3.up * spawnHeight;
    }

    private float GetExplosionRadius(WeaponLevelData data)
    {
        return data.ExplosionRadius;
    }

    private float GetMeteorDelay(WeaponLevelData data)
    {
        return data.MeteorDelay;
    }
}
