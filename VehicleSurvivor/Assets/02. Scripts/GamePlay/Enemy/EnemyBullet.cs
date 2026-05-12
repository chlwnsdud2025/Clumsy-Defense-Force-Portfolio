using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 endPos;
    private float arcHeight;
    private float duration;
    private float progress;
    private float damage;
    private float explosionRadius;

    [Header("Hit Settings")]
    public LayerMask playerLayer; // 인스펙터에서 플레이어 레이어를 꼭 할당해주세요!

    public void InitializeParabolic(Vector3 start, Vector3 end, float height, float damage, float flyTime, float radius)
    {
        this.startPos = start;
        this.endPos = end;
        this.arcHeight = height;
        this.damage = damage;
        this.duration = flyTime;
        this.explosionRadius = radius;
        this.progress = 0f;
    }

    void Update()
    {
        progress += Time.deltaTime / duration;

        // X, Z 축 일직선 이동
        Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
        // Y 축에 Sine파를 곱해 포물선을 그림
        currentPos.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;

        transform.position = currentPos;

        // 도착 시 폭발
        if (progress >= 1.0f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        // 닿았을 때가 아니라 도착 지점에서 폭발 반경(구체) 내의 모든 타겟에 데미지 판정
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius, playerLayer);

        foreach (Collider hit in hitColliders)
        {
            IDamageable target = hit.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }
        }

        Destroy(gameObject); // (임시) 타격 후 파괴
    }

    // 에디터 화면에서 폭발 범위를 빨간 선으로 보여줍니다.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        //Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}