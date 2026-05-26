using UnityEngine;

public class DroneProjectile
    : MonoBehaviour
{
    [Header("Homing")]
    public float rotateSpeed = 8f;

    private LayerMask targetLayer;

    private float damage;
    private float speed;
    private float maxDistance;

    private Vector3 moveDirection;
    private Vector3 spawnPosition;

    private Transform target;
    private float detectRadius;

    public void Initialize(
        LayerMask newTargetLayer,
        float newDamage,
        float newSpeed,
        float newMaxDistance,
        Vector3 newMoveDirection,
        Transform newTarget,
        float newDetectRadius
    )
    {
        targetLayer =
            newTargetLayer;

        damage =
            newDamage;

        speed =
            newSpeed;

        maxDistance =
            newMaxDistance;

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
        if (target == null)
        {
            target =
                FindClosestTarget();
        }

        if (target != null)
        {
            Vector3 targetPos =
                target.position;

            targetPos.y += 0.8f;

            Vector3 targetDirection =
                targetPos -
                transform.position;

            if (
                targetDirection !=
                Vector3.zero
            )
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(
                        targetDirection.normalized
                    );

                transform.rotation =
                    Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        rotateSpeed *
                        Time.deltaTime
                    );

                moveDirection =
                    transform.forward;
            }
        }

        transform.position +=
            moveDirection *
            speed *
            Time.deltaTime;

        if (
            Vector3.Distance(
                spawnPosition,
                transform.position
            ) >= maxDistance
        )
        {
            Destroy(gameObject);
        }
    }

    private Transform FindClosestTarget()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                detectRadius,
                targetLayer
            );

        Transform closest =
            null;

        float closestDistance =
            Mathf.Infinity;

        foreach (
            Collider hit
            in hits
        )
        {
            float distance =
                (
                    hit.transform.position -
                    transform.position
                ).sqrMagnitude;

            if (
                distance <
                closestDistance
            )
            {
                closestDistance =
                    distance;

                closest =
                    hit.transform;
            }
        }

        return closest;
    }

    private void OnTriggerEnter(
    Collider other
)
    {
        Transform root =
            other.transform.root;

        if (root.gameObject.layer ==
            LayerMask.NameToLayer("Player"))
        {
            return;
        }

        IDamageable damageable =
            other.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        damageable.TakeDamage(
            damage
        );
        Debug.Log($"드론 데미지 들어감: {damage}");

        Destroy(gameObject);
    }
}