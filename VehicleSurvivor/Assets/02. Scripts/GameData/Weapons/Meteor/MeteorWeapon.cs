using UnityEngine;

public class MeteorWeapon : WeaponBase
{
    [Header("메테오 데이터")]
    public MeteorWeaponDataSO meteorData;

    [Header("레이어 설정")]
    public LayerMask enemyLayer;

    [Header("임시 테스트 스탯")]
    public float damage = 40f;
    public float cooldown = 3f;
    public float explosionRadius = 5f;
    public float meteorDelay = 0.3f;

    [Header("생성 위치 설정")]
    public float spawnRadius = 15f;
    public float spawnHeight = 12f;
    public float groundY = 0f;

    [Header("안전 거리")]
    public float safeRadius = 7f;
    public int maxTryCount = 20;

    [Header("대각선 낙하")]
    public float diagonalOffset = 12f;
    public float randomAngleRange = 20f;

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
        if (meteorData == null || meteorData.meteorPrefab == null)
            return;

        Vector3 targetPosition = GetRandomTargetPosition();
        Vector3 spawnPosition = GetDiagonalSpawnPosition(targetPosition);

        GameObject meteor = Instantiate(
            meteorData.meteorPrefab,
            spawnPosition,
            Quaternion.identity
        );

        MeteorProjectile projectile =
            meteor.GetComponent<MeteorProjectile>();

        if (projectile != null)
        {
            projectile.Init(
                GetFinalDamage(damage),
                explosionRadius,
                meteorDelay,
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

        float randomAngle = Random.Range(
            -randomAngleRange,
            randomAngleRange
        );

        Vector3 finalDirection =
            Quaternion.Euler(0f, randomAngle, 0f) * baseDirection;

        return targetPosition
               + finalDirection * diagonalOffset
               + Vector3.up * spawnHeight;
    }
}