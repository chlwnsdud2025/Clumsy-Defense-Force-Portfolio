using UnityEngine;

public class HomingProjectileBullet : MonoBehaviour
{
    [Header("유도탄 설정")]
    public float damage = 10f;
    public float speed = 10f;
    public float rotateSpeed = 10f;
    public float lifeTime = 5f;
    public float hitDistance = 1f;

    private Transform target;

    private void Start()
    {
        FindTarget();
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (target == null)
        {
            FindTarget();

            transform.position += transform.forward * speed * Time.deltaTime;
            return;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.magnitude <= hitDistance)
        {
            HitTarget();
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );

        transform.position += transform.forward * speed * Time.deltaTime;
    }

    public void Init(float newDamage, float newSpeed)
    {
        damage = newDamage;
        speed = newSpeed;
    }

    private void FindTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        float closestDistance = Mathf.Infinity;
        Transform closestTarget = null;

        foreach (GameObject enemy in enemies)
        {
            float distance =
                (enemy.transform.position - transform.position).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = enemy.transform;
            }
        }

        target = closestTarget;
    }

    private void HitTarget()
    {
        if (target != null)
        {
            Enemy enemy = target.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponentInParent<Enemy>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}