using System.Collections.Generic;
using UnityEngine;

public class RudolphSleighProjectile : MonoBehaviour
{
    private float damage;
    private float speed;
    private float travelDistance;

    private Vector3 moveDirection;
    private Vector3 spawnPosition;

    private readonly HashSet<IDamageable>
        damagedTargets =
        new HashSet<IDamageable>();

    public void Initialize(
        float newDamage,
        float newSpeed,
        float newTravelDistance,
        Vector3 newMoveDirection
    )
    {
        damage =
            newDamage;

        speed =
            newSpeed;

        travelDistance =
            newTravelDistance;

        moveDirection =
            newMoveDirection.normalized;

        spawnPosition =
            transform.position;
    }

    private void Update()
    {
        transform.position +=
            moveDirection *
            speed *
            Time.deltaTime;

        float movedDistance =
            Vector3.Distance(
                spawnPosition,
                transform.position
            );

        if (
            movedDistance >=
            travelDistance
        )
        {
            Destroy(gameObject);
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

        if (
            damagedTargets.Contains(
                damageable
            )
        )
        {
            return;
        }

        damagedTargets.Add(
            damageable
        );

        damageable.TakeDamage(
            damage
        );
        Debug.Log($"썰매 데미지 들어감: {damage}");
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