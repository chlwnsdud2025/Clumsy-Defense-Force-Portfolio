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
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position +=
            moveDirection *
            speed *
            Time.deltaTime;
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
                Quaternion.LookRotation(
                    moveDirection
                );
        }
    }

    private void OnTriggerEnter(
        Collider other
    )
    {
        if (
            !other.CompareTag(
                "Enemy"
            )
        )
            return;

        Enemy enemy =
            other
            .GetComponentInParent
            <Enemy>();

        if (enemy != null)
        {
            enemy.TakeDamage(
                damage
            );

            Debug.Log(
                $"Projectile Damage : {damage}"
            );
        }

        Destroy(gameObject);
    }
}