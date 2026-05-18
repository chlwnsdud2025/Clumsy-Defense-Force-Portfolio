using UnityEngine;

public class MeteorWeapon : WeaponBase
{
    [Header("메테오 설정")]
    public GameObject meteorPrefab;
    public LayerMask enemyLayer;

    [Header("생성 위치 설정")]
    public float spawnRadius = 15f;
    public float spawnHeight = 12f;

    [Header("바닥 설정")]
    public float groundY = 0f;

    [Header("안전 거리")]
    public float safeRadius = 7f;
    public int maxTryCount = 20;

    [Header("대각선 낙하 설정")]
    public float diagonalOffset = 12f;
    public float randomAngleRange = 20f;

    protected override void Attack(WeaponLevelData data)
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

        MeteorProjectile meteorProjectile =
            meteor.GetComponent<MeteorProjectile>();

        if (meteorProjectile != null)
        {
            meteorProjectile.Init(
                GetFinalDamage(data.damage),
                data.explosionRadius,
                data.meteorDelay,
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

            float distance =
                Vector3.Distance(transform.position, randomPosition);

            if (distance >= safeRadius)
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
        Vector3 baseDirection =
            new Vector3(-1f, 0f, 1f).normalized;

        float randomAngle =
            Random.Range(-randomAngleRange, randomAngleRange);

        Vector3 finalDirection =
            Quaternion.Euler(0f, randomAngle, 0f) * baseDirection;

        Vector3 spawnPosition =
            targetPosition +
            finalDirection * diagonalOffset +
            Vector3.up * spawnHeight;

        return spawnPosition;
    }
}