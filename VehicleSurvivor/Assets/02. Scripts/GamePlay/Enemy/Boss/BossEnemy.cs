using UnityEngine;

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

    // 자식 클래스에서 패턴 시전 중임을 알릴 때 사용하는 플래그
    protected bool isCastingPattern = false;

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        currentAttackCooldown = attackCooltime;

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }
    }

    protected virtual void Update()
    {
        if (playerTarget == null) return;

        float dt = Time.deltaTime;

        // 1. 공격 쿨타임 계산 및 Attack 실행
        if (!isCastingPattern)
        {
            currentAttackCooldown -= dt;
            if (currentAttackCooldown <= 0)
            {
                Attack();
            }
        }

        // 2. 이동 및 지형 추적 (패턴 시전 중에도 멈추지 않음)
        MoveAndSnapToGround(dt);
    }

    // 자식 클래스(개별 보스들)가 반드시 자신만의 패턴으로 덮어써야 하는 공격 함수!
    protected abstract void Attack();

    // ==========================================
    // 공통 지형 이동 로직 (모든 보스가 공유)
    // ==========================================
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
            RaycastHit[] hits = Physics.RaycastAll(highRayStart, Vector3.down, raycastDistance, groundLayer);

            if (hits.Length > 0)
            {
                float closestDiff = float.MaxValue;
                for (int i = 0; i < hits.Length; i++)
                {
                    float diff = Mathf.Abs(hits[i].point.y - playerTarget.position.y);
                    if (diff < closestDiff) { closestDiff = diff; targetHeight = hits[i].point.y; hitGround = true; }
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
                RaycastHit[] hits = Physics.RaycastAll(highRayStart, Vector3.down, raycastDistance, groundLayer);
                if (hits.Length > 0)
                {
                    float closestDiff = float.MaxValue;
                    for (int i = 0; i < hits.Length; i++)
                    {
                        float diff = Mathf.Abs(hits[i].point.y - playerTarget.position.y);
                        if (diff < closestDiff) { closestDiff = diff; targetHeight = hits[i].point.y; hitGround = true; }
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

    
    // 공통 체력 관리 로직
    public virtual void TakeDamage(float damage)
    {
        currentHealth -= damage;
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
}