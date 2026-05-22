using UnityEngine;

public class HomingProjectile : MonoBehaviour
{
    [Header("Homing")]
    public float rotateSpeed = 10f;

    private float damage;
    private float speed;
    private float maxTravelDistance;

    private Vector3 moveDirection;
    private Vector3 spawnPosition;

    private Transform target;
    private float detectRadius;

    public void Initialize(
        float newDamage,
        float newSpeed,
        float newMaxTravelDistance,
        Vector3 newMoveDirection,
        Transform newTarget,
        float newDetectRadius
    )
    {
        damage = newDamage;
        speed = newSpeed;
        maxTravelDistance =
            newMaxTravelDistance;

        moveDirection =
            newMoveDirection.normalized;

        spawnPosition =
            transform.position;

        target =
            newTarget;

        detectRadius =
            newDetectRadius;
    }

    private void Update()
    {
        UpdateTarget();

        if (target != null)
        {
            Vector3 targetDirection =
                (
                    target.position -
                    transform.position
                ).normalized;

            moveDirection =
                Vector3.Lerp(
                    moveDirection,
                    targetDirection,
                    rotateSpeed *
                    Time.deltaTime
                ).normalized;

            transform.rotation =
                Quaternion.LookRotation(
                    moveDirection
                );
        }

        transform.position +=
            moveDirection *
            speed *
            Time.deltaTime;

        float distance =
            Vector3.Distance(
                spawnPosition,
                transform.position
            );

        if (distance >=
            maxTravelDistance)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateTarget()
    {
        if (target != null)
            return;

        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                detectRadius
            );

        float closestDistance =
            Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            float distance =
                (
                    hit.transform.position -
                    transform.position
                ).sqrMagnitude;

            if (distance <
                closestDistance)
            {
                closestDistance =
                    distance;

                target =
                    hit.transform;
            }
        }
    }

    private void OnTriggerEnter(
        Collider other
    )
    {
        IDamageable damageable =
            other.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        damageable.TakeDamage(
            damage
        );

        Destroy(gameObject);
    }
}