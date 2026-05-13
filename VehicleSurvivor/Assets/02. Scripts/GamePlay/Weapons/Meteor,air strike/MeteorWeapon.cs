using UnityEngine;

public class MeteorWeapon : WeaponBase
{
    [Header("메테오 설정")]
    public GameObject meteorPrefab;

    public LayerMask enemyLayer;

    public float spawnRadius = 15f;
    public float spawnHeight = 20f;

    protected override void Attack(WeaponLevelData data)
    {
        if (meteorPrefab == null)
            return;

        Vector2 randomCircle =
            Random.insideUnitCircle * spawnRadius;

        Vector3 targetPosition =
            transform.position +
            new Vector3(randomCircle.x, 0f, randomCircle.y);

        Vector3 spawnPosition =
            targetPosition + Vector3.up * spawnHeight;

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
}