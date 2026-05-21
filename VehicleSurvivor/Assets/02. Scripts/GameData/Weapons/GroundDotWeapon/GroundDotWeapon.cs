using UnityEngine;

public class GroundDotWeapon : WeaponBase
{
    [Header("장판 설정")]
    public GameObject groundDotPrefab;
    public LayerMask enemyLayer;

    [Header("생성 위치")]
    public float spawnForwardDistance = 2f;
    public float groundY = 0f;

    protected override void Attack(WeaponLevelData data)
    {
        if (groundDotPrefab == null)
            return;

        Vector3 spawnPosition =
            transform.position + transform.forward * spawnForwardDistance;

        spawnPosition.y = groundY;

        GameObject area = Instantiate(
            groundDotPrefab,
            spawnPosition,
            transform.rotation
        );

        GroundDotArea dotArea =
            area.GetComponent<GroundDotArea>();

        if (dotArea != null)
        {
            dotArea.Init(
                GetFinalDamage(data.Damage),
                data.DotDuration,
                data.DotTickInterval,
                data.DotWidth,
                data.DotLength,
                enemyLayer
            );
        }
    }
}
