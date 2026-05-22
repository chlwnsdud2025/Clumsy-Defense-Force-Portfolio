using UnityEngine;

public class ProjectileBullet : MonoBehaviour
{
    private float damage;
    private float speed;
    private int pierceCount;
    private float maxTravelDistance;

    private Vector3 moveDirection;
    private Vector3 spawnPosition;

    public void Initialize(
        float newDamage,
        float newSpeed,
        int newPierceCount,
        float newMaxTravelDistance,
        Vector3 newMoveDirection
    )
    {
        damage = newDamage;
        speed = newSpeed;
        pierceCount = newPierceCount;
        maxTravelDistance = newMaxTravelDistance;

        moveDirection = newMoveDirection.normalized;
        spawnPosition = transform.position;
    }

    private void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;

        float traveledDistance =
            Vector3.Distance(spawnPosition, transform.position);

        if (traveledDistance >= maxTravelDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        IDamageable damageable =
            other.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        damageable.TakeDamage(damage);

        if (pierceCount > 0)
        {
            pierceCount--;
            return;
        }

        Destroy(gameObject);
    }
}