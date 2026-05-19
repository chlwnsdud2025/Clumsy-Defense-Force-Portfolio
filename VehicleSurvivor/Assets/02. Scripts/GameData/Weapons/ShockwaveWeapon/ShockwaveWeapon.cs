using UnityEngine;

public class ShockwaveWeapon : WeaponBase
{
    [Header("충격파 설정")]
    public LayerMask enemyLayer;

    [Header("이펙트 설정")]
    public GameObject shockwaveEffectPrefab;
    public float effectLifeTime = 2f;

    [Header("생성 위치")]
    public float spawnForwardDistance = 1.5f;

    protected override void Attack(WeaponLevelData data)
    {
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;

        Collider[] hits = Physics.OverlapSphere(
            origin,
            data.shockwaveRange,
            enemyLayer
        );

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
                continue;

            Enemy enemy = hit.GetComponentInParent<Enemy>();

            if (enemy == null)
                continue;

            Vector3 directionToEnemy =
                enemy.transform.position - origin;

            directionToEnemy.y = 0f;

            if (directionToEnemy == Vector3.zero)
                continue;

            float angle = Vector3.Angle(
                forward,
                directionToEnemy.normalized
            );

            if (angle <= data.shockwaveAngle * 0.5f)
            {
                enemy.TakeDamage(
                    GetFinalDamage(data.damage)
                );

                Debug.Log(
                    $"[Shockwave] {enemy.name} 피격 / 데미지: {GetFinalDamage(data.damage)}"
                );
            }
        }

        SpawnEffect(data);
    }

    private void SpawnEffect(WeaponLevelData data)
    {
        if (shockwaveEffectPrefab == null)
            return;

        Vector3 spawnPosition =
            transform.position +
            transform.forward * spawnForwardDistance;

        GameObject effect = Instantiate(
            shockwaveEffectPrefab,
            spawnPosition,
            transform.rotation
        );

        float size = data.shockwaveRange;

        effect.transform.localScale =
            new Vector3(size, 1f, size);

        Destroy(effect, effectLifeTime);
    }

    private void OnDrawGizmosSelected()
    {
        WeaponLevelData data = GetCurrentLevelData();

        if (data == null)
            return;

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            data.shockwaveRange
        );
    }
}