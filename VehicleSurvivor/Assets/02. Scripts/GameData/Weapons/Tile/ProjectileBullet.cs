using UnityEngine;

public class ProjectileBullet : MonoBehaviour
{
    [Header("투사체 설정")]
    public float damage = 10f;
    public float speed = 20f;
    public float lifeTime = 3f;

    private Vector3 moveDirection;

    private void Start()
    {
        if (moveDirection == Vector3.zero)
        {
            moveDirection = transform.forward;
        }

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    public void Init(
        float newDamage,
        float newSpeed,
        Vector3 direction
    )
    {
        damage = newDamage;
        speed = newSpeed;
        moveDirection = direction.normalized;

        if (moveDirection != Vector3.zero)
        {
            transform.rotation =
                Quaternion.LookRotation(moveDirection);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        IDamageable damageable =
            other.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        damageable.TakeDamage(damage);

        Debug.Log($"[Projectile] 데미지: {damage}");

        Destroy(gameObject);
    }
}