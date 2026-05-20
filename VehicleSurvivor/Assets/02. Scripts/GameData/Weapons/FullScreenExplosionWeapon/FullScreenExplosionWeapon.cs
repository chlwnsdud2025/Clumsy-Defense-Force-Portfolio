using UnityEngine;

public class FullScreenExplosionWeapon : WeaponBase
{
    [Header("전체 공격 설정")]
    public LayerMask enemyLayer;

    [Header("탐색 설정")]
    public float searchRadius = 100f;

    [Header("이펙트 설정")]
    public GameObject explosionEffectPrefab;
    public float effectLifeTime = 3f;

    protected override void Attack(WeaponLevelData data)
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            searchRadius,
            enemyLayer
        );

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
                continue;

            Enemy enemy = hit.GetComponentInParent<Enemy>();

            if (enemy == null)
                continue;

            enemy.TakeDamage(
                GetFinalDamage(data.Damage)
            );

            Debug.Log(
                $"[FullScreenExplosion] {enemy.name} 피격 / 데미지: {GetFinalDamage(data.Damage)}"
            );
        }

        SpawnEffect();
    }

    private void SpawnEffect()
    {
        if (explosionEffectPrefab == null)
            return;

        GameObject effect = Instantiate(
            explosionEffectPrefab,
            transform.position,
            Quaternion.identity
        );

        Destroy(effect, effectLifeTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;

        Gizmos.DrawWireSphere(
            transform.position,
            searchRadius
        );
    }
}
