using UnityEngine;

public class MeteorProjectile
    : MonoBehaviour
{
    [Header("Effect")]
    public GameObject explosionEffectPrefab;

    private float damage;
    private float speed;
    private float explosionRadius;
    private float delay;

    private Vector3 targetPosition;

    private bool isFalling;

    public void Initialize(
    float newDamage,
    float newSpeed,
    float newExplosionRadius,
    float newDelay,
    Vector3 newTargetPosition
)
    {
        damage = newDamage;
        speed = newSpeed;
        explosionRadius = newExplosionRadius;
        delay = newDelay;

        targetPosition = newTargetPosition;

        Vector3 direction =
            (
                targetPosition -
                transform.position
            ).normalized;

        transform.rotation =
            Quaternion.LookRotation(direction);

        Invoke(
            nameof(StartFall),
            delay
        );
    }

    private void StartFall()
    {
        isFalling = true;
    }

    private void Update()
    {
        if (!isFalling)
            return;

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                targetPosition,
                speed *
                Time.deltaTime
            );

        float distance =
            Vector3.Distance(
                transform.position,
                targetPosition
            );

        if (distance <= 0.1f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                explosionRadius
            );

        foreach (Collider hit in hits)
        {
            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            damageable.TakeDamage(
                damage
            );
        }

        if (explosionEffectPrefab != null)
        {
            Instantiate(
                explosionEffectPrefab,
                transform.position,
                explosionEffectPrefab.transform.rotation
            );
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            explosionRadius
        );
    }
}