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
        float shockwaveRange = GetFinalWeaponScale(data.ShockwaveRange);

        Collider[] hits = Physics.OverlapSphere(
            origin,
            shockwaveRange,
            enemyLayer
        );

        DamageInfo damageInfo = GetFinalDamage(data.Damage);

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

            if (angle <= data.ShockwaveAngle * 0.5f)
            {
                enemy.TakeDamage(damageInfo);

                Debug.Log(
                    $"[Shockwave] {enemy.name} 피격 / 데미지: {damageInfo.Amount}"
                );
            }
        }

        SpawnEffect(data, shockwaveRange);
    }

    private void SpawnEffect(WeaponLevelData data, float shockwaveRange)
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

        effect.transform.localScale =
            new Vector3(shockwaveRange, 1f, shockwaveRange);

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
            GetFinalWeaponScale(data.ShockwaveRange)
        );
    }
}
