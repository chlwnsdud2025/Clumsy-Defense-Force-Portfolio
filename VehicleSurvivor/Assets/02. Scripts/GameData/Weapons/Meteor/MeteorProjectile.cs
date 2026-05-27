using UnityEngine;

public class MeteorProjectile : MonoBehaviour
{
    [Header("Effect")]
    public GameObject explosionEffectPrefab;

    [Header("Effect Life Time")]
    public float explosionEffectLifeTime = 3f;

    private DamageInfo damageInfo;
    private float speed;
    private float explosionRadius;
    private float delay;

    private Vector3 moveDirection; // 추가: 낙하할 방향 벡터
    private bool isFalling;
    private bool hasExploded;

    public void Initialize(
        DamageInfo newDamageInfo,
        float newSpeed,
        float newExplosionRadius,
        float newDelay,
        Vector3 newTargetPosition
    )
    {
        damageInfo = newDamageInfo;
        speed = newSpeed;
        explosionRadius = newExplosionRadius;
        delay = newDelay;

        // [수정] 강제로 특정 위치로 이동하는 것이 아니라, 목표 지점을 향하는 '방향'만 계산해 둡니다.
        moveDirection = (newTargetPosition - transform.position).normalized;

        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }

        Invoke(nameof(StartFall), delay);
    }

    private void StartFall()
    {
        isFalling = true;
    }

    private void Update()
    {
        if (!isFalling || hasExploded)
            return;

        // [수정] 목표 위치 도달 여부와 상관없이 계산된 방향으로 계속 낙하합니다.
        transform.position += moveDirection * speed * Time.deltaTime;

        // [안전장치] 만약 맵 밖으로 빗나가서 영원히 떨어지는 것을 방지 (y가 너무 낮아지면 삭제)
        if (transform.position.y < -50f)
        {
            Destroy(gameObject);
        }
    }

    // [추가] 물리 충돌로 바닥을 감지하여 폭발시킵니다.
    private void OnTriggerEnter(Collider other)
    {
        if (hasExploded)
            return;

        // 부딪힌 오브젝트의 레이어가 "Ground"인지 확인합니다.
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (hasExploded)
            return;

        hasExploded = true;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            explosionRadius
        );

        foreach (Collider hit in hits)
        {
            if (IsPlayerLayer(hit))
                continue;

            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            damageable.TakeDamage(damageInfo);

            Debug.Log($"[Meteor] {hit.name} 에게 {damageInfo.Amount} 데미지");
        }

        SoundPlayback.PlaySfx(ESoundId.SFX_Weapon_Explosion);

        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                explosionEffectPrefab,
                transform.position,
                explosionEffectPrefab.transform.rotation
            );

            Destroy(effect, explosionEffectLifeTime);
        }

        Destroy(gameObject);
    }

    private bool IsPlayerLayer(Collider other)
    {
        int playerLayer = LayerMask.NameToLayer("Player");

        if (playerLayer == -1)
            return false;

        if (other.gameObject.layer == playerLayer)
            return true;

        Transform root = other.transform.root;

        return root.gameObject.layer == playerLayer;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            explosionRadius
        );
    }
}