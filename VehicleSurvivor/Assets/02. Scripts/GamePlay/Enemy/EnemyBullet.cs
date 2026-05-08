using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    private Vector3 moveDirection;
    private float speed;
    private float damage;
    private float currentLifeTime;

    // 총알이 발사될 때 RangedEnemy가 호출해 줄 초기화 함수
    public void Initialize(Vector3 direction, float speed, float damage, float lifeTime = 5f)
    {
        moveDirection = direction.normalized;
        this.speed = speed;
        this.damage = damage;
        this.currentLifeTime = lifeTime; // 일정 시간이 지나면 스스로 파괴되도록 설정
    }

    void Update()
    {
        // 1. 앞으로 이동 (물리 엔진 없이 수학적 이동으로 최적화)
        transform.position += moveDirection * speed * Time.deltaTime;

        // 2. 수명 체크 (화면 밖으로 영원히 날아가서 메모리를 갉아먹는 것 방지)
        currentLifeTime -= Time.deltaTime;
        if (currentLifeTime <= 0)
        {
            Destroy(gameObject); // (임시) 나중에는 파괴 대신 풀(Pool)로 반환해야 합니다.
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 3. 차량(플레이어)에 닿았는지 체크하여 데미지 적용
        IDamageable target = other.GetComponent<IDamageable>();

        // 주의: 다른 몹을 때리지 않도록 태그 검사를 추가하는 것이 좋습니다.
        if (target != null && other.CompareTag("Player"))
        {
            target.TakeDamage(damage);
            Destroy(gameObject); // (임시) 타격 후 파괴
        }
    }
}