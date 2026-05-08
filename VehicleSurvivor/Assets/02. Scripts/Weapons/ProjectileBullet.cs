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
        if (!other.CompareTag("Enemy")) return;

       // EnemyHealth enemyHealth = other.GetComponentInParent<EnemyHealth>();

       // if (enemyHealth != null)
        //{
          //  enemyHealth.TakeDamage(damage);
       // }

        Destroy(gameObject);
    }
}