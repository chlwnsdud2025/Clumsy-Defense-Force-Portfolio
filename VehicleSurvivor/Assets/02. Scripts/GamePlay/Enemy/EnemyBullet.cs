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
    public LayerMask playerLayer;

    // [변경됨] 인스펙터에서 설정하지 않고, Initialize 시점에 전달받습니다.
    private GameObject explosionVfx;

    // [수정됨] 매개변수에 explosionVfx 추가
    public void InitializeParabolic(Vector3 start, Vector3 end, float height, float damage, float flyTime, float radius, GameObject vfx)
    {
        this.startPos = start;
        this.endPos = end;
        this.arcHeight = height;
        this.damage = damage;
        this.duration = flyTime;
        this.explosionRadius = radius;
        this.explosionVfx = vfx; // 넘겨받은 이펙트 저장
        this.progress = 0f;
    }

    void Update()
    {
        progress += Time.deltaTime / duration;

        Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
        currentPos.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;

        transform.position = currentPos;

        if (progress >= 1.0f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        // [수정됨] 넘겨받은 이펙트가 있다면 생성
        if (explosionVfx != null)
        {
            Instantiate(explosionVfx, transform.position, Quaternion.identity);
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius, playerLayer);

        foreach (Collider hit in hitColliders)
        {
            IDamageable target = hit.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }
}