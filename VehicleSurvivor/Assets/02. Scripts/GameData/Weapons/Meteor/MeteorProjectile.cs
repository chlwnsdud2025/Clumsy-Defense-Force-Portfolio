using UnityEngine;

public class MeteorProjectile : MonoBehaviour
{
    [Header("Effect")]
    public GameObject explosionEffectPrefab;

    [Header("Effect Life Time")]
    public float explosionEffectLifeTime = 3f;

    private DamageInfo damageInfo;
    private float speed;
    private float explosionRadius;
    private float delay;

    private Vector3 targetPosition;
    private bool isFalling;
    private bool hasExploded;

    public void Initialize(
        DamageInfo newDamageInfo,
        float newSpeed,
        float newExplosionRadius,
        float newDelay,
        Vector3 newTargetPosition
    )
    {
        damageInfo = newDamageInfo;
        speed = newSpeed;
        explosionRadius = newExplosionRadius;
        delay = newDelay;

        targetPosition = newTargetPosition;

        Vector3 direction =
            (targetPosition - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            transform.rotation =
                Quaternion.LookRotation(direction);
        }

        Invoke(nameof(StartFall), delay);
    }

    private void StartFall()
    {
        isFalling = true;
    }

    private void Update()
    {
        if (!isFalling || hasExploded)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) <= 0.1f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (hasExploded)
            return;

        hasExploded = true;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            explosionRadius
        );

        foreach (Collider hit in hits)
        {
            if (IsPlayerLayer(hit))
                continue;

            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            damageable.TakeDamage(damageInfo);

            Debug.Log($"[Meteor] {hit.name} 에게 {damageInfo.Amount} 데미지");
        }

        SoundPlayback.PlaySfx(ESoundId.SFX_Weapon_Explosion);

        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                explosionEffectPrefab,
                transform.position,
                explosionEffectPrefab.transform.rotation
            );

            Destroy(effect, explosionEffectLifeTime);
        }

        Destroy(gameObject);
    }

    private bool IsPlayerLayer(Collider other)
    {
        int playerLayer =
            LayerMask.NameToLayer("Player");

        if (playerLayer == -1)
            return false;

        if (other.gameObject.layer == playerLayer)
            return true;

        Transform root = other.transform.root;

        return root.gameObject.layer == playerLayer;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            explosionRadius
        );
    }
}