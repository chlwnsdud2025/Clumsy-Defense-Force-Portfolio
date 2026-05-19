using UnityEngine;

public class DroneHomingProjectile : MonoBehaviour
{
    [Header("유도탄 설정")]
    public float damage = 10f;
    public float speed = 15f;
    public float rotateSpeed = 10f;
    public float lifeTime = 5f;
    public float hitDistance = 0.8f;
    public float retargetRadius = 10f;

    [Header("레이어 설정")]
    public LayerMask enemyLayer;

    private Transform target;

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
        LayerMask newEnemyLayer
    )
    {
        target = firstTarget;
        damage = newDamage;
        speed = newSpeed;
        enemyLayer = newEnemyLayer;
    }

    private void FindNewTarget()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            retargetRadius,
            enemyLayer
        );

        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
                continue;

            Enemy enemy = hit.GetComponentInParent<Enemy>();

            if (enemy == null)
                continue;

            float distance =
                (enemy.transform.position - transform.position).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = enemy.transform;
            }
        }

        target = closestTarget;
    }

    private void HitTarget()
    {
        if (target != null)
        {
            Enemy enemy = target.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponentInParent<Enemy>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}