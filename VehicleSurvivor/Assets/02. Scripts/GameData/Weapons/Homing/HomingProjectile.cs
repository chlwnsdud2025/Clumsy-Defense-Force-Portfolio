using UnityEngine;

public class HomingProjectile : MonoBehaviour
{
    [Header("유도탄 설정")]
    public float damage = 10f;
    public float speed = 15f;
    public float rotateSpeed = 10f;
    public float lifeTime = 5f;
    public float hitDistance = 0.8f;
    public float retargetRadius = 10f;

    private Transform target;
    private LayerMask targetLayer;
    private GameObject hitEffectPrefab;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            FindNewTarget();
        }

        if (target == null)
        {
            transform.position += transform.forward * speed * Time.deltaTime;
            return;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.magnitude <= hitDistance)
        {
            HitTarget();
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(direction.normalized);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );

        transform.position += transform.forward * speed * Time.deltaTime;
    }

    public void Init(
        Transform firstTarget,
        float newDamage,
        float newSpeed,
        LayerMask newTargetLayer,
        GameObject newHitEffectPrefab
    )
    {
        target = firstTarget;
        damage = newDamage;
        speed = newSpeed;
        targetLayer = newTargetLayer;
        hitEffectPrefab = newHitEffectPrefab;
    }

    private void FindNewTarget()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            retargetRadius,
            targetLayer
        );

        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            float distance =
                (hit.transform.position - transform.position).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = hit.transform;
            }
        }

        target = closestTarget;
    }

    private void HitTarget()
    {
        if (target != null)
        {
            IDamageable damageable =
                target.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            SpawnHitEffect(target.position);
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        IDamageable damageable =
            other.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        damageable.TakeDamage(damage);

        SpawnHitEffect(transform.position);

        Destroy(gameObject);
    }

    private void SpawnHitEffect(Vector3 position)
    {
        if (hitEffectPrefab == null)
            return;

        GameObject effect = Instantiate(
            hitEffectPrefab,
            position,
            Quaternion.identity
        );

        Destroy(effect, 2f);
    }
}