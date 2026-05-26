using UnityEngine;
using System.Collections.Generic;

public class ChasingTornado : MonoBehaviour
{
    private Transform target;
    private float speed;
    private float damage;
    private float radius;
    private float tickInterval;
    private LayerMask targetLayer;

    private float tickTimer = 0f;

    // ==========================================
    // [몹과 동일한 지형 추적 로직 변수들]
    // ==========================================
    [Header("Terrain & Movement")]
    [Tooltip("지형으로 인식할 레이어를 선택하세요 (몹들과 동일하게 세팅)")]
    public LayerMask groundLayer;
    public float raycastStartHeight = 50f;
    public float raycastDistance = 100f;
    public float fastStepSpeed = 25f;
    public float slowClimbSpeed = 5f;
    public float gravity = -20f;

    private float verticalVelocity;
    private bool isGrounded;
    private RaycastHit[] hitBuffer = new RaycastHit[10];
    // ==========================================

    public void Initialize(Transform playerTarget, float moveSpeed, float tickDamage, float hitRadius, float duration, float damageInterval, LayerMask layer)
    {
        target = playerTarget;
        speed = moveSpeed;
        damage = tickDamage;
        radius = hitRadius;
        tickInterval = damageInterval;
        targetLayer = layer;

        Destroy(gameObject, duration);
    }

    void Update()
    {
        if (target == null) return;

        float dt = Time.deltaTime;

        // 1. [신규 적용] 몹과 완벽히 동일한 지형 밀착 이동 로직!
        MoveAndSnapToGround(dt);

        // 2. 다단히트(Tick) 데미지 연산
        tickTimer += dt;
        if (tickTimer >= tickInterval)
        {
            tickTimer = 0f;
            ExecuteDamage();
        }
    }

    // BossEnemy에서 사용하던 극강 최적화 이동 로직을 회오리에 맞게 이식했습니다.
    private void MoveAndSnapToGround(float dt)
    {
        Vector3 playerDir = target.position - transform.position;
        playerDir.y = 0;
        playerDir = playerDir.normalized;

        Vector3 horizontalVelocity = playerDir * speed;

        Vector3 nextPosXZ = transform.position + (horizontalVelocity * dt);
        float targetHeight = transform.position.y;
        bool hitGround = false;

        bool isPlayerHigher = target.position.y > transform.position.y + 2.0f;
        if (isPlayerHigher)
        {
            float dynamicHeight = Mathf.Max(transform.position.y + raycastStartHeight, target.position.y + 5.0f);
            Vector3 highRayStart = nextPosXZ + Vector3.up * dynamicHeight;

            int hitCount = Physics.RaycastNonAlloc(highRayStart, Vector3.down, hitBuffer, raycastDistance, groundLayer);

            if (hitCount > 0)
            {
                float closestDiff = float.MaxValue;
                for (int i = 0; i < hitCount; i++)
                {
                    float diff = Mathf.Abs(hitBuffer[i].point.y - target.position.y);
                    if (diff < closestDiff) { closestDiff = diff; targetHeight = hitBuffer[i].point.y; hitGround = true; }
                }
            }
        }
        else
        {
            float lowRayStartHeight = 2.0f;
            Vector3 lowRayStart = nextPosXZ + Vector3.up * lowRayStartHeight;
            if (Physics.Raycast(lowRayStart, Vector3.down, out RaycastHit lowHit, 4.0f, groundLayer))
            {
                targetHeight = lowHit.point.y; hitGround = true;
            }
            else
            {
                Vector3 highRayStart = nextPosXZ + Vector3.up * raycastStartHeight;

                int hitCount = Physics.RaycastNonAlloc(highRayStart, Vector3.down, hitBuffer, raycastDistance, groundLayer);

                if (hitCount > 0)
                {
                    float closestDiff = float.MaxValue;
                    for (int i = 0; i < hitCount; i++)
                    {
                        float diff = Mathf.Abs(hitBuffer[i].point.y - target.position.y);
                        if (diff < closestDiff) { closestDiff = diff; targetHeight = hitBuffer[i].point.y; hitGround = true; }
                    }
                }
            }
        }

        if (hitGround)
        {
            float heightDiff = targetHeight - transform.position.y;
            if (heightDiff >= -0.1f)
            {
                float currentClimbSpeed = (heightDiff > 0.5f) ? slowClimbSpeed : fastStepSpeed;
                if (heightDiff > 0.5f) horizontalVelocity = Vector3.zero; // 벽에 막히면 전진 중단

                float newY = Mathf.MoveTowards(transform.position.y, targetHeight, currentClimbSpeed * dt);
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
                verticalVelocity = 0;
                isGrounded = true;
            }
            else isGrounded = false;
        }
        else isGrounded = false;

        if (!isGrounded)
        {
            verticalVelocity += gravity * dt;
            if (hitGround && transform.position.y + (verticalVelocity * dt) < targetHeight)
            {
                transform.position = new Vector3(transform.position.x, targetHeight, transform.position.z);
                verticalVelocity = 0;
                isGrounded = true;
            }
        }

        transform.position += (horizontalVelocity + Vector3.up * verticalVelocity) * dt;
    }

    private void ExecuteDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, targetLayer);
        foreach (Collider hit in hits)
        {
            hit.GetComponent<IDamageable>()?.TakeDamage(damage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}