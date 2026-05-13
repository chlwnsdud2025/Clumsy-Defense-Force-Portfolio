using UnityEngine;

public class ProjectileBullet : MonoBehaviour
{
    public float damage = 10f;
    public float lifeTime = 3f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponentInParent<Enemy>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Debug.Log("damage: " + damage);
        }

        Destroy(gameObject);
    }
}