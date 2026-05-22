using UnityEngine;
using System.Collections; // [추가] 코루틴 사용을 위해 필요

// 잡몹(Enemy)과는 완전히 분리된, 보스들만의 새로운 최상위 부모 클래스!
public abstract class BossEnemy : MonoBehaviour, IDamageable
{
    [Header("Base References")]
    public Transform playerTarget;

    [Header("Base Stats")]
    public float maxHealth = 10f;
    protected float currentHealth;
    public float moveSpeed = 3f;
    public float rotationSpeed = 3f;

    [Header("Combat Base")]
    public float contactDamage = 30f;
    public float ContactDamage => contactDamage;

    [Header("Terrain & Movement (스마트 지형 추적)")]
    public LayerMask groundLayer;
    public float raycastStartHeight = 50f;
    public float raycastDistance = 100f;
    public float fastStepSpeed = 25f;
    public float slowClimbSpeed = 5f;
    public float gravity = -20f;

    private float verticalVelocity;
    private bool isGrounded;

    [Header("Combat Base")]
    public float attackCooltime = 4.0f;
    protected float currentAttackCooldown;

    private RaycastHit[] hitBuffer = new RaycastHit[10];
    // 자식 클래스에서 패턴 시전 중임을 알릴 때 사용하는 플래그
    protected bool isCastingPattern = false;

    // ==========================================
    // [신규] 피격 점멸 (Hit Flash) 설정
    // ==========================================
    [Header("Hit Flash Settings")]
    [Tooltip("색상을 바꿀 보스의 모델링 렌더러들을 넣어주세요.")]
    public Renderer[] modelRenderers;
    [Tooltip("하얗게 유지되는 시간")]
    public float flashDuration = 0.1f;
    [Tooltip("다음 점멸까지의 쿨타임 (이 시간 동안은 맞아도 안 번쩍임)")]
    public float flashCooldown = 0.1f;

    private Color[] originalColors;
    private bool isFlashing = false;
    // ==========================================

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        currentAttackCooldown = attackCooltime;

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }

        // [신규] 보스의 원래 색상들을 기억해 둡니다.
        if (modelRenderers != null && modelRenderers.Length > 0)
        {
            originalColors = new Color[modelRenderers.Length];
            for (int i = 0; i < modelRenderers.Length; i++)
            {
                if (modelRenderers[i] != null)
                    originalColors[i] = modelRenderers[i].material.color;
            }
        }
    }

    protected virtual void Update()
    {
        if (playerTarget == null) return;

        float dt = Time.deltaTime;

        if (!isCastingPattern)
        {
            currentAttackCooldown -= dt;
            if (currentAttackCooldown <= 0)
            {
                Attack();
            }
        }

        MoveAndSnapToGround(dt);
    }

    protected abstract void Attack();

    // (지형 이동 로직은 동일하게 유지)
    private void MoveAndSnapToGround(float dt)
    {
        Vector3 playerDir = playerTarget.position - transform.position;
        playerDir.y = 0;
        float distanceToPlayer = playerDir.magnitude;
        playerDir = playerDir.normalized;

        Vector3 horizontalVelocity = playerDir * moveSpeed;

        Vector3 nextPosXZ = transform.position + (horizontalVelocity * dt);
        float targetHeight = transform.position.y;
        bool hitGround = false;

        bool isPlayerHigher = playerTarget.position.y > transform.position.y + 2.0f;
        if (isPlayerHigher)
        {
            float dynamicHeight = Mathf.Max(transform.position.y + raycastStartHeight, playerTarget.position.y + 5.0f);
            Vector3 highRayStart = nextPosXZ + Vector3.up * dynamicHeight;

            // [최적화 적용] RaycastAll 대신 RaycastNonAlloc 사용! (hitBuffer 상자 재활용)
            int hitCount = Physics.RaycastNonAlloc(highRayStart, Vector3.down, hitBuffer, raycastDistance, groundLayer);

            // 상자에 담긴 개수(hitCount)만큼만 검사합니다.
            if (hitCount > 0)
            {
                float closestDiff = float.MaxValue;
                for (int i = 0; i < hitCount; i++)
                {
                    float diff = Mathf.Abs(hitBuffer[i].point.y - playerTarget.position.y);
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

                // [최적화 적용] 여기도 동일하게 교체해 줍니다.
                int hitCount = Physics.RaycastNonAlloc(highRayStart, Vector3.down, hitBuffer, raycastDistance, groundLayer);

                if (hitCount > 0)
                {
                    float closestDiff = float.MaxValue;
                    for (int i = 0; i < hitCount; i++)
                    {
                        float diff = Mathf.Abs(hitBuffer[i].point.y - playerTarget.position.y);
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
                if (heightDiff > 0.5f) horizontalVelocity = Vector3.zero;

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

        if (playerDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(playerDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * dt);
        }
    }


    public virtual void TakeDamage(float damage)
    {
        currentHealth -= damage;

        TriggerHitFlash(); // 점멸 효과 발동!

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    protected virtual void Die()
    {
        Debug.Log($"{gameObject.name} 보스 처치 완료!");
        StageRunManager.Instance.OnMidBossDefeated();
        Destroy(gameObject);
    }

    
    protected void TriggerHitFlash()
    {
        if (!isFlashing && modelRenderers != null && modelRenderers.Length > 0)
        {
            StartCoroutine(HitFlashRoutine());
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        isFlashing = true;

        for (int i = 0; i < modelRenderers.Length; i++)
        {
            if (modelRenderers[i] != null)
                modelRenderers[i].material.color = Color.white;
        }

        yield return new WaitForSeconds(flashDuration);

        if (modelRenderers != null && originalColors != null)
        {
            for (int i = 0; i < modelRenderers.Length; i++)
            {
                if (modelRenderers[i] != null)
                    modelRenderers[i].material.color = originalColors[i];
            }
        }

        yield return new WaitForSeconds(flashCooldown);

        isFlashing = false;
    }
}