using UnityEngine;

public class CarBumper : MonoBehaviour
{
    [Header("Bumper Settings")]
    public float knockbackMultiplier = 1.5f; // 밀어내는 힘 배율
    public float minSpeedToKnockback = 1.0f; // 이 속도 이상 달릴 때만 몹이 밀림 (정차 시 밀림 방지)

    private Vector3 lastPosition;
    private float actualSpeed;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        // Player.cs를 건드릴 수 없으므로, 스스로 위치 변화량을 측정해 실제 속도를 구합니다.
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        actualSpeed = distanceMoved / Time.deltaTime;
        lastPosition = transform.position;
    }

    void OnTriggerStay(Collider other)
    {
        // 차가 일정 속도 이상 달리고 있을 때, 태그가 "Enemy"인 오브젝트와 닿으면
        if (actualSpeed > minSpeedToKnockback && other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                // 차에서 적을 향하는 방향으로 밀어냅니다.
                Vector3 pushDir = other.transform.position - transform.position;

                // 차의 현재 속도에 비례해서 밀어내는 파워를 결정합니다.
                float pushPower = actualSpeed * knockbackMultiplier;

                enemy.ApplyKnockback(pushDir, pushPower);
            }
        }
    }
}