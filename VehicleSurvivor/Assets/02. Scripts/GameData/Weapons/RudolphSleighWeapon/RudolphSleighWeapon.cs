using UnityEngine;

public class RudolphSleighWeapon : WeaponBase
{
    [Header("Sleigh References")]
    public GameObject sleighPrefab;

    [Header("Spawn Option")]
    public float spawnHeight = 0.5f;
    public float groundY = 0f;

    [Header("Rotation")]
    public Vector3 sleighRotationOffset;

    protected override void Attack(WeaponLevelData data)
    {
        if (data == null)
            return;

        SpawnSleigh(
            GetFinalDamage(data.Damage),
            GetSleighSpeed(data),
            data.Range,
            data.Radius,
            data.Size
        );
    }

    private void SpawnSleigh(
        float finalDamage,
        float moveSpeed,
        float travelDistance,
        float spawnRadius,
        float size
    )
    {
        if (sleighPrefab == null)
        {
            Debug.LogWarning("[RudolphSleighWeapon] Sleigh Prefab 없음");
            return;
        }

        if (spawnRadius <= 0f)
            spawnRadius = 10f;

        if (travelDistance <= 0f)
            travelDistance = 20f;

        // 1. 플레이어 주변 랜덤 위치
        Vector2 randomPositionCircle =
            Random.insideUnitCircle.normalized;

        if (randomPositionCircle == Vector2.zero)
            randomPositionCircle = Vector2.right;

        Vector3 spawnDirectionFromPlayer =
            new Vector3(
                randomPositionCircle.x,
                0f,
                randomPositionCircle.y
            ).normalized;

        Vector3 spawnPosition =
            transform.position +
            spawnDirectionFromPlayer * spawnRadius;

        // 바닥 위로 고정
        spawnPosition.y = groundY + spawnHeight;

        // 2. 플레이어를 향하는 게 아니라 랜덤 방향으로 질주
        Vector2 randomMoveCircle =
            Random.insideUnitCircle.normalized;

        if (randomMoveCircle == Vector2.zero)
            randomMoveCircle = Vector2.right;

        Vector3 moveDirection =
            new Vector3(
                randomMoveCircle.x,
                0f,
                randomMoveCircle.y
            ).normalized;

        // 3. 이동 방향을 바라보게 회전
        Quaternion moveRotation =
            Quaternion.LookRotation(moveDirection);

        Quaternion visualRotation =
            moveRotation *
            Quaternion.Euler(sleighRotationOffset);

        GameObject sleighObject =
            Instantiate(
                sleighPrefab,
                spawnPosition,
                visualRotation
            );

        // 프리팹 원래 스케일 유지
        sleighObject.transform.localScale =
            sleighPrefab.transform.localScale * size;

        RudolphSleighProjectile sleigh =
            sleighObject.GetComponent<RudolphSleighProjectile>();

        if (sleigh == null)
        {
            Debug.LogWarning("[RudolphSleighWeapon] RudolphSleighProjectile 없음");
            return;
        }

        sleigh.Initialize(
            finalDamage,
            moveSpeed,
            travelDistance,
            moveDirection
        );
    }

    private float GetSleighSpeed(WeaponLevelData data)
    {
        return data.ProjectileSpeed > 0f
            ? data.ProjectileSpeed
            : data.BulletSpeed;
    }
}