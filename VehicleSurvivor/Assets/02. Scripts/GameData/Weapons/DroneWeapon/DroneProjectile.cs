using UnityEngine;

public class DroneProjectile : MonoBehaviour
{
    [Header("Homing")]
    public float rotateSpeed = 8f;

    private float damage;
    private float speed;
    private float maxDistance;

    private Vector3 moveDirection;
    private Vector3 spawnPosition;

    private Transform target;
    private float detectRadius;

    public void Initialize(
        float newDamage,
        float newSpeed,
        float newMaxDistance,
        Vector3 newMoveDirection,
        Transform newTarget,
        float newDetectRadius
    )
    {
        damage = newDamage;
        speed = newSpeed;
        maxDistance = newMaxDistance;

        moveDirection = newMoveDirection.normalized;
        spawnPosition = transform.position;

        target = newTarget;
        detectRadius = newDetectRadius;
    }

    private void Update()
    {
        if (target == null)
        {
            target = FindClosestTarget();
        }

        if (target != null)
        {
            Vector3 targetPosition = target.position;
            targetPosition.y += 0.8f; // 적 몸통 높이 보정

            Vector3 targetDirection =
                targetPosition - transform.position;

            if (targetDirection != Vector3.zero)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(targetDirection.normalized);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotateSpeed * Time.deltaTime
                );

                moveDirection = transform.forward;
            }
        }

        transform.position +=
            moveDirection * speed * Time.deltaTime;

        if (Vector3.Distance(spawnPosition, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private Transform FindClosestTarget()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectRadius
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

        return closestTarget;
    }

    private void OnTriggerEnter(Collider other)
    {
        IDamageable damageable =
            other.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        damageable.TakeDamage(damage);

        Destroy(gameObject);
    }
}