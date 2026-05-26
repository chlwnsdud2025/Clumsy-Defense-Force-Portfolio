using UnityEngine;

public class BouncyProjectile : MonoBehaviour
{
    private float damage;
    private float maxTravelDistance;
    private int bounceCount;

    private float gravity;
    private float bounciness;

    private Vector3 velocity;
    private Vector3 spawnPosition;

    public void Initialize(
        float newDamage,
        Vector3 startVelocity,
        float newMaxTravelDistance,
        int newBounceCount,
        LayerMask newBounceLayer,
        float newGravity,
        float newBounciness
    )
    {
        damage = newDamage;
        velocity = startVelocity;

        maxTravelDistance =
            newMaxTravelDistance;

        bounceCount =
            newBounceCount;

        gravity =
            newGravity;

        bounciness =
            Mathf.Clamp01(
                newBounciness
            );

        spawnPosition =
            transform.position;

        if (
            maxTravelDistance <= 0f
        )
        {
            maxTravelDistance = 20f;
        }
    }

    private void Update()
    {
        velocity +=
            Vector3.down *
            gravity *
            Time.deltaTime;

        transform.position +=
            velocity *
            Time.deltaTime;

        if (velocity != Vector3.zero)
        {
            transform.rotation =
                Quaternion.LookRotation(
                    velocity.normalized
                );
        }

        float traveledDistance =
            Vector3.Distance(
                spawnPosition,
                transform.position
            );

        if (
            traveledDistance >=
            maxTravelDistance
        )
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(
        Collider other
    )
    {
        // 플레이어만 무시
        if (IsPlayerLayer(other))
            return;

        // 적이면 데미지
        IDamageable damageable =
            other.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(
                damage
            );

            Debug.Log(
                $"[BouncyProjectile] {other.name} 에게 {damage} 데미지"
            );
        }

        // 플레이어 제외 모든 오브젝트 Bounce
        Bounce(other);
    }

    private void Bounce(
        Collider other
    )
    {
        if (bounceCount <= 0)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 normal =
            GetBounceNormal(other);

        velocity =
            Vector3.Reflect(
                velocity,
                normal
            ) * bounciness;

        bounceCount--;

        Debug.Log(
            $"[BouncyProjectile] {other.name} 충돌 → Bounce 남은 횟수: {bounceCount}"
        );
    }

    private Vector3 GetBounceNormal(
        Collider other
    )
    {
        Vector3 closestPoint =
            other.ClosestPoint(
                transform.position
            );

        Vector3 normal =
            (
                transform.position -
                closestPoint
            ).normalized;

        if (normal == Vector3.zero)
        {
            normal = Vector3.up;
        }

        return normal;
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