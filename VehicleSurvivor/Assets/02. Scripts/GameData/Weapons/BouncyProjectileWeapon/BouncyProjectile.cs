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

    private SphereCollider sphereCollider;

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

        maxTravelDistance = newMaxTravelDistance;
        bounceCount = newBounceCount;

        gravity = newGravity;
        bounciness = Mathf.Clamp01(newBounciness);

        spawnPosition = transform.position;

        sphereCollider = GetComponent<SphereCollider>();

        if (maxTravelDistance <= 0f)
            maxTravelDistance = 20f;
    }

    private void Update()
    {
        velocity += Vector3.down * gravity * Time.deltaTime;

        Vector3 moveDelta = velocity * Time.deltaTime;

        if (moveDelta != Vector3.zero)
        {
            float radius = GetSphereRadius();

            if (Physics.SphereCast(
                    transform.position,
                    radius,
                    moveDelta.normalized,
                    out RaycastHit hit,
                    moveDelta.magnitude
                ))
            {
                if (!IsPlayerLayer(hit.collider))
                {
                    transform.position =
                        hit.point + hit.normal * radius;

                    HandleHit(hit.collider, hit.normal);
                    return;
                }
            }
        }

        transform.position += moveDelta;

        if (velocity != Vector3.zero)
        {
            transform.rotation =
                Quaternion.LookRotation(velocity.normalized);
        }

        float traveledDistance =
            Vector3.Distance(spawnPosition, transform.position);

        if (traveledDistance >= maxTravelDistance)
        {
            Destroy(gameObject);
        }
    }

    private void HandleHit(Collider other, Vector3 normal)
    {
        if (IsPlayerLayer(other))
            return;

        IDamageable damageable =
            other.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(damage);

            Debug.Log(
                $"[BouncyProjectile] {other.name} 에게 {damage} 데미지"
            );
        }

        BounceByNormal(normal, other);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerLayer(other))
            return;

        IDamageable damageable =
            other.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(damage);

            Debug.Log(
                $"[BouncyProjectile] {other.name} 에게 {damage} 데미지"
            );

            Bounce(other);
        }
    }

    private void Bounce(Collider other)
    {
        Vector3 normal = GetBounceNormal(other);
        BounceByNormal(normal, other);
    }

    private void BounceByNormal(Vector3 normal, Collider other)
    {
        if (bounceCount <= 0)
        {
            Destroy(gameObject);
            return;
        }

        velocity =
            Vector3.Reflect(velocity, normal) *
            bounciness;

        bounceCount--;

        Debug.Log(
            $"[BouncyProjectile] {other.name} 충돌 → Bounce 남은 횟수: {bounceCount}"
        );
    }

    private float GetSphereRadius()
    {
        if (sphereCollider == null)
            sphereCollider = GetComponent<SphereCollider>();

        if (sphereCollider == null)
            return 0.25f;

        float maxScale = Mathf.Max(
            transform.lossyScale.x,
            transform.lossyScale.y,
            transform.lossyScale.z
        );

        return sphereCollider.radius * maxScale;
    }

    private Vector3 GetBounceNormal(Collider other)
    {
        Vector3 closestPoint =
            other.ClosestPoint(transform.position);

        Vector3 normal =
            (transform.position - closestPoint).normalized;

        if (normal == Vector3.zero)
            normal = Vector3.up;

        return normal;
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
}