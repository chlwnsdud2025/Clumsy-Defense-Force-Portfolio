using UnityEngine;

public class BouncyProjectile : MonoBehaviour
{
    [Header("탱탱볼 데이터")]
    public float damage = 10f;
    public float speed = 20f;
    public int bounceCount = 1;
    public float lifeTime = 5f;

    private LayerMask enemyLayer;
    private LayerMask bounceLayer;

    private Vector3 moveDirection;

    public void Init(
        float newDamage,
        float newSpeed,
        int newBounceCount,
        LayerMask newEnemyLayer,
        LayerMask newBounceLayer,
        Vector3 direction
    )
    {
        damage = newDamage;
        speed = newSpeed;
        bounceCount = newBounceCount;
        enemyLayer = newEnemyLayer;
        bounceLayer = newBounceLayer;
        moveDirection = direction.normalized;

        transform.rotation = Quaternion.LookRotation(moveDirection);

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        int otherLayerMask = 1 << other.gameObject.layer;

        if ((enemyLayer.value & otherLayerMask) != 0)
        {
            Enemy enemy = other.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Debug.Log($"[Bouncy] {enemy.name} 피격 / 데미지: {damage}");
            }

            return;
        }

        if ((bounceLayer.value & otherLayerMask) != 0)
        {
            Bounce(other);
        }
    }

    private void Bounce(Collider other)
    {
        if (bounceCount <= 0)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 closestPoint = other.ClosestPoint(transform.position);
        Vector3 normal = (transform.position - closestPoint).normalized;

        if (normal == Vector3.zero)
        {
            normal = -moveDirection;
        }

        moveDirection = Vector3.Reflect(moveDirection, normal).normalized;
        transform.rotation = Quaternion.LookRotation(moveDirection);

        bounceCount--;
    }
}