using UnityEngine;

public class StraightBullet : MonoBehaviour
{
    public float speed = 20f;
    public float maxDistance = 40f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // 정면 방향으로 이동
        transform.position += transform.forward * speed * Time.deltaTime;

        // 일정 거리 이상 날아가면 삭제
        float distance = Vector3.Distance(startPos, transform.position);

        if (distance >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Enemy 맞으면 삭제
        if (other.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
    }
}
