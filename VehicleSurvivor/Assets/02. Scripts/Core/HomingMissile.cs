using UnityEngine;

public class HomingMissile : MonoBehaviour
{
    public float speed = 15f;
    public float rotateSpeed = 5f;

    private Transform target;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;

        FindTarget();
        Destroy(gameObject, 20f);
    }

    void Update()
    {
        if (target != null)
        {
            // 방향 계산
            Vector3 dir = (target.position - transform.position).normalized;

            // 회전
            Quaternion lookRot = Quaternion.LookRotation(dir);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                lookRot,
                rotateSpeed * Time.deltaTime
            );
        }

        // 앞으로 이동
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void FindTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        float closestDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);

            if (dist < closestDistance)
            {
                closestDistance = dist;
                target = enemy.transform;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
         // enemy.TakeDamage(10);
            Destroy(gameObject);
        }
    }
   


}