using UnityEngine;

public class GroundDotWeapon : WeaponBase
{

    //    사용 스탯:
    //Damage
    //Cooldown = 장판 생성 간격
    //DotDuration = 장판 유지 시간
    //DotTickInterval = 피해 주기
    //DotWidth = 장판 너비
    //DotLength = 장판 길이
    [Header("Ground Dot References")]
    public GameObject groundDotAreaPrefab;

    [Header("Spawn Option")]
    public float minMoveDistance = 0.5f;
    public float groundOffsetY = 0.05f;

    private Vector3 lastSpawnPosition;
    private bool hasSpawnedOnce;

    protected override void Attack(WeaponLevelData data)
    {
        if (data == null)
            return;

        TrySpawnGroundDot(data);
    }

    private void TrySpawnGroundDot(WeaponLevelData data)
    {
        if (groundDotAreaPrefab == null)
        {
            Debug.LogWarning("[GroundDotWeapon] Ground Dot Area Prefab 없음");
            return;
        }

        Vector3 currentPosition = transform.position;

        if (hasSpawnedOnce)
        {
            float movedDistance =
                Vector3.Distance(
                    lastSpawnPosition,
                    currentPosition
                );

            if (movedDistance < minMoveDistance)
                return;
        }

        Vector3 spawnPosition = currentPosition;
        spawnPosition.y += groundOffsetY;

        GameObject areaObject = Instantiate(
            groundDotAreaPrefab,
            spawnPosition,
            transform.rotation
        );

        GroundDotArea area =
            areaObject.GetComponent<GroundDotArea>();

        if (area == null)
        {
            Debug.LogWarning("[GroundDotWeapon] GroundDotArea 컴포넌트 없음");
            return;
        }

        area.Initialize(
            GetFinalDamage(data.Damage),
            data.DotDuration,
            data.DotTickInterval,
            data.DotWidth,
            data.DotLength
        );

        lastSpawnPosition = currentPosition;
        hasSpawnedOnce = true;
    }
}