using UnityEngine;

public class ProjectileBullet : MonoBehaviour
{
    private int damage;
    private float speed;
    private float range;

    private bool canPierce;
    private int pierceCount;

    private bool canExplode;
    private float explosionRadius;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    public void Setup(WeaponLevelData data)
    {
        damage = data.damage;
        speed = data.projectileSpeed;
        range = data.range;

        canPierce = data.canPierce;
        pierceCount = data.pierceCount;

        canExplode = data.canExplode;
        explosionRadius = data.explosionRadius;
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;

        float movedDistance = Vector3.Distance(startPosition, transform.position);

        if (movedDistance >= range)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        // 상대 팀원이 만든 EnemyHealth에 TakeDamage(int)가 있으면 그대로 사용 가능
        other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

        if (canExplode)
        {
            Explode();
        }

        if (canPierce)
        {
            pierceCount--;

            if (pierceCount <= 0)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Explode()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                hit.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            }
        }

        Destroy(gameObject);
    }
}