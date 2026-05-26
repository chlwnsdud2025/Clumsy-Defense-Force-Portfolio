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

        if (maxTravelDistance <= 0f)
        {
            maxTravelDistance = 20f;
        }
    }

    private void Update()
    {
        transform.position +=
            moveDirection * speed * Time.deltaTime;

        float traveledDistance =
            Vector3.Distance(spawnPosition, transform.position);

        if (traveledDistance >= maxTravelDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerLayer(other))
            return;

        IDamageable damageable =
            other.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        damageable.TakeDamage(damage);
        Debug.Log($"당근 데미지 들어감: {damage}");

        if (pierceCount > 0)
        {
            pierceCount--;
            return;
        }

        Destroy(gameObject);
    }

    private bool IsPlayerLayer(Collider other)
    {
        int playerLayer =
            LayerMask.NameToLayer("Player");

        if (playerLayer == -1)
            return false;

        if (other.gameObject.layer == playerLayer)
            return true;

        Transform root =
            other.transform.root;

        return root.gameObject.layer == playerLayer;
    }
}