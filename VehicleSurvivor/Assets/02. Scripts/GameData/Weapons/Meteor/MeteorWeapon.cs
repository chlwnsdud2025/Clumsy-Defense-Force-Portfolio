using UnityEngine;

public class MeteorWeapon : WeaponBase
{
    [Header("Meteor Data")]
    public MeteorWeaponDataSO meteorData;

    [Header("Target Settings")]
    public LayerMask enemyLayer;

    [Header("Legacy Stats")]
    public float damage = 40f;
    public float cooldown = 3f;
    public float explosionRadius = 5f;
    public float meteorDelay = 0.3f;

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
        SpawnMeteor(
            GetFinalDamage(data.Damage),
            GetExplosionRadius(data),
            GetMeteorDelay(data)
        );
    }

    private void AttackLegacy()
    {
        SpawnMeteor(
            GetFinalDamage(damage),
            explosionRadius,
            meteorDelay
        );
    }

    private void SpawnMeteor(
        float finalDamage,
        float finalExplosionRadius,
        float finalMeteorDelay
    )
    {
        if (meteorData == null || meteorData.meteorPrefab == null)
            return;

        Vector3 targetPosition = GetRandomTargetPosition();
        Vector3 spawnPosition = GetDiagonalSpawnPosition(targetPosition);

        GameObject meteor = Instantiate(
            meteorData.meteorPrefab,
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
        if (data == null)
            return explosionRadius;

        return data.explosionRadius > 0f ? data.explosionRadius : explosionRadius;
    }

    private float GetMeteorDelay(WeaponLevelData data)
    {
        if (data == null)
            return meteorDelay;

        return data.meteorDelay > 0f ? data.meteorDelay : meteorDelay;
    }
}
