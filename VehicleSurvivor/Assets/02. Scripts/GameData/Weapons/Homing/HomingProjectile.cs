using UnityEngine;

public class HomingProjectile : MonoBehaviour
{
    [Header("Homing")]
    public float rotateSpeed = 10f;

    private DamageInfo damageInfo;
    private float speed;
    private float maxTravelDistance;

    private Vector3 moveDirection;
    private Vector3 spawnPosition;

    private Transform target;
    private float detectRadius;

    public void Initialize(
        DamageInfo newDamageInfo,
        float newSpeed,
        float newMaxTravelDistance,
        Vector3 newMoveDirection,
        Transform newTarget,
        float newDetectRadius
    )
    {
        damageInfo = newDamageInfo;
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

        if (
            distance >=
            maxTravelDistance
        )
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
            if (IsPlayerLayer(hit))
                continue;

            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

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

                target =
                    hit.transform;
            }
        }
    }

    private void OnTriggerEnter(
        Collider other
    )
    {
        if (IsPlayerLayer(other))
            return;

        IDamageable damageable =
            other.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        damageable.TakeDamage(
            damageInfo
        );
        Debug.Log($"비행기 데미지 들어감: {damageInfo.Amount}");

        Destroy(gameObject);
    }

    private bool IsPlayerLayer(
        Collider other
    )
    {
        int playerLayer =
            LayerMask.NameToLayer(
                "Player"
            );

        if (playerLayer == -1)
            return false;

        if (
            other.gameObject.layer ==
            playerLayer
        )
        {
            return true;
        }

        Transform root =
            other.transform.root;

        return
            root.gameObject.layer ==
            playerLayer;
    }
}