using UnityEngine;

public class MeteorProjectile : MonoBehaviour
{
    [Header("이펙트 설정")]
    public GameObject explosionEffectPrefab;

    [Header("메테오 설정")]
    public float damage;
    public float explosionRadius;
    public float fallSpeed = 25f;

    private float delayTimer;
    private float meteorDelay;

    private LayerMask enemyLayer;
    private Vector3 targetPosition;

    private bool isFalling;

    public void Init(
        float newDamage,
        float newExplosionRadius,
        float newMeteorDelay,
        LayerMask newEnemyLayer,
        Vector3 newTargetPosition
    )
    {
        damage = newDamage;
        explosionRadius = newExplosionRadius;
        meteorDelay = newMeteorDelay;
        enemyLayer = newEnemyLayer;

        targetPosition = newTargetPosition;
    }

    private void Update()
    {
        if (!isFalling)
        {
            delayTimer += Time.deltaTime;

            if (delayTimer >= meteorDelay)
            {
                isFalling = true;
            }

            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            fallSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) < 0.2f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        Collider[] hits = Physics.OverlapSphere(
            targetPosition,
            explosionRadius,
            enemyLayer
        );

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
                continue;

            Enemy enemy = hit.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);

                Debug.Log($"[Meteor] {enemy.name} 피격 / 데미지: {damage}");
            }
        }

        if (explosionEffectPrefab != null)
        {
            Vector3 effectPosition = targetPosition;
            effectPosition.y += 0.2f;

            GameObject effect = Instantiate(
                explosionEffectPrefab,
                effectPosition,
                Quaternion.identity
            );

            Destroy(effect, 3f);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetPosition, explosionRadius);
    }
}