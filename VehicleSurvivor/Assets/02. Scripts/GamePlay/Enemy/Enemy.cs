using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage);
    float ContactDamage { get; }
}

public class Enemy : MonoBehaviour, IDamageable
{
    protected Transform playerTarget;
    protected EnemyManager myManager;

    [Header("UI")]
    public Transform damageTextSpawnpoint;

    [HideInInspector] public EnemyPoolDataSO originPool;

    [Header("Stat")]
    public float moveSpeed = 5f;
    public float maxHealth = 10;
    protected float currentHealth;
    public float rotationSpeed = 2f;

    [Header("Exp Reward")]
    public int expReward = 34;

    [Header("Separation Settings")]
    public float separationRadius = 1.5f;
    public float separationWeight = 0.5f;

    [Header("Behavior Settings")]
    public float attackRange = 2.0f;
    public float attackCooltime = 2.0f;

    [Header("3D Ground Snapping & Gravity")]
    public LayerMask groundLayer;         // 지형으로 인식할 레이어
    public float raycastStartHeight = 50f; // 레이캐스트를 쏠 상공의 높이
    public float raycastDistance = 100f;   // 레이캐스트 최대 도달 거리
    public float fastStepSpeed = 25f;      // 평지, 계단, 작은 돌멩이를 밟고 지나가는 빠른 속도
    public float slowClimbSpeed = 5f;      // 절벽, 다리 기둥 등을 천천히 기어오르는 속도

    [Header("Attack")]
    public float contactDamage = 10f;
    public float ContactDamage => contactDamage;

    public float gravity = -20f; // 중력

    protected bool isTrackingPlayer = true;

    protected float currentAttackCooldown;
    protected Vector3 knockbackVelocity;

    // 수직 이동(점프/추락) 처리를 위한 상태 변수
    private float verticalVelocity;
    private bool isGrounded;

    // ==========================================
    // [최적화] 지형 탐색(Raycast) 변수
    // ==========================================
    private float groundCheckTimer = 0f;
    private float groundCheckInterval = 0.1f; // 0.1초마다 1번씩만 지형 탐색 (1초에 10번)
    private RaycastHit[] hitBuffer = new RaycastHit[10]; // GC 생성을 막기 위한 미리 만들어둔 버퍼 배열
    private float cachedTargetHeight; // 탐색 간격 동안 유지할 목표 높이
    private bool cachedHitGround;     // 탐색 간격 동안 유지할 접지 여부

    public virtual void SpawnSetup(Transform target, Vector3 spawnPosition, EnemyManager manager)
    {
        myManager = manager;
        playerTarget = target;
        transform.position = spawnPosition;
        gameObject.SetActive(true);
        currentHealth = maxHealth;
        knockbackVelocity = Vector3.zero;
        currentAttackCooldown = 0f;

        // 스폰 시 수직 속도 및 접지 상태 초기화
        verticalVelocity = 0f;
        isGrounded = false;

        // [최적화] 몹들이 동시에 레이캐스트를 쏴서 렉이 걸리는 것을 방지하기 위해 초기 타이머를 분산시킵니다.
        groundCheckTimer = Random.Range(0f, groundCheckInterval);
        cachedTargetHeight = spawnPosition.y;
        cachedHitGround = true;
    }

    public virtual void ManagedUpdate(float deltaTime)
    {
        if (playerTarget == null) return;

        // 수평 이동 방향 및 속도 계산 (X, Z)
        ExecuteBehavior(deltaTime, out Vector3 desiredVelocity);

        Vector3 separationForce = myManager.GetSeparationForce(this, separationRadius);
        separationForce.y = 0;

        Vector3 horizontalVelocity = desiredVelocity + (separationForce * separationWeight);

        // ==========================================
        // [최적화된] 다중 레이어 투시 스마트 레이캐스트
        // ==========================================
        groundCheckTimer += deltaTime;

        // 매 프레임이 아닌 지정된 간격(Interval)마다 한 번씩만 물리 연산 수행
        if (groundCheckTimer >= groundCheckInterval)
        {
            groundCheckTimer = 0f;

            // 0.1초 뒤의 대략적인 예상 위치로 레이캐스트를 쏩니다.
            Vector3 checkPosXZ = transform.position + (horizontalVelocity * groundCheckInterval);
            cachedHitGround = false;
            cachedTargetHeight = transform.position.y;

            bool isPlayerHigher = playerTarget.position.y > transform.position.y + 2.0f;

            if (isPlayerHigher)
            {
                // [플레이어가 높은 곳에 있음]
                float dynamicHeight = Mathf.Max(transform.position.y + raycastStartHeight, playerTarget.position.y + 5.0f);
                Vector3 highRayStart = checkPosXZ + Vector3.up * dynamicHeight;

                // RaycastAll 대신 RaycastNonAlloc 사용 -> 메모리 쓰레기 발생량 0
                int hitCount = Physics.RaycastNonAlloc(highRayStart, Vector3.down, hitBuffer, raycastDistance, groundLayer);

                if (hitCount > 0)
                {
                    float closestDiff = float.MaxValue;
                    for (int i = 0; i < hitCount; i++) // hitBuffer 전체가 아닌 hitCount 까지만 순회
                    {
                        float diff = Mathf.Abs(hitBuffer[i].point.y - playerTarget.position.y);
                        if (diff < closestDiff)
                        {
                            closestDiff = diff;
                            cachedTargetHeight = hitBuffer[i].point.y;
                            cachedHitGround = true;
                        }
                    }
                }
            }
            else
            {
                // [플레이어가 평지이거나 내 아래에 있음]
                float lowRayStartHeight = 2.0f;
                float lowRayDistance = 4.0f;
                Vector3 lowRayStart = checkPosXZ + Vector3.up * lowRayStartHeight;

                if (Physics.Raycast(lowRayStart, Vector3.down, out RaycastHit lowHit, lowRayDistance, groundLayer))
                {
                    cachedTargetHeight = lowHit.point.y;
                    cachedHitGround = true;
                }
                else
                {
                    Vector3 highRayStart = checkPosXZ + Vector3.up * raycastStartHeight;
                    int hitCount = Physics.RaycastNonAlloc(highRayStart, Vector3.down, hitBuffer, raycastDistance, groundLayer);

                    if (hitCount > 0)
                    {
                        float closestDiff = float.MaxValue;
                        for (int i = 0; i < hitCount; i++)
                        {
                            float diff = Mathf.Abs(hitBuffer[i].point.y - playerTarget.position.y);
                            if (diff < closestDiff)
                            {
                                closestDiff = diff;
                                cachedTargetHeight = hitBuffer[i].point.y;
                                cachedHitGround = true;
                            }
                        }
                    }
                }
            }
        }


        // 지형 충돌 결과 처리(단차) - 이동 연산은 매 프레임 부드럽게 적용됩니다.
        if (cachedHitGround)
        {
            float heightDiff = cachedTargetHeight - transform.position.y;

            if (heightDiff >= -0.1f)
            {
                float currentClimbSpeed;

                if (heightDiff > 0.5f)
                {
                    horizontalVelocity = Vector3.zero; // 거대한 벽을 만나면 천천히 등반
                    currentClimbSpeed = slowClimbSpeed;
                }
                else
                {
                    currentClimbSpeed = fastStepSpeed; // 작은 단차는 빠른 속도로 보행
                }

                float newY = Mathf.MoveTowards(transform.position.y, cachedTargetHeight, currentClimbSpeed * deltaTime);
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);

                verticalVelocity = 0;
                isGrounded = true;
            }
            else // 낭떠러지
            {
                isGrounded = false;
            }
        }
        else
        {
            isGrounded = false;
        }

        // 중력 적용
        if (!isGrounded)
        {
            verticalVelocity += gravity * deltaTime;

            if (cachedHitGround && transform.position.y + (verticalVelocity * deltaTime) < cachedTargetHeight)
            {
                transform.position = new Vector3(transform.position.x, cachedTargetHeight, transform.position.z);
                verticalVelocity = 0;
                isGrounded = true;
            }
        }

        // 최종 위치 적용
        transform.position += (horizontalVelocity + Vector3.up * verticalVelocity) * deltaTime;

        // 회전 처리
        if (isTrackingPlayer)
        {
            Vector3 playerDir = playerTarget.position - transform.position;
            playerDir.y = 0;

            if (playerDir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(playerDir.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * deltaTime);
            }
        }
    }

    protected virtual void ExecuteBehavior(float deltaTime, out Vector3 desiredVelocity)
    {
        Vector3 playerDir = playerTarget.position - transform.position;
        playerDir.y = 0;
        float distanceToPlayer = playerDir.magnitude;
        playerDir = playerDir.normalized;

        desiredVelocity = Vector3.zero;

        if (currentAttackCooldown > 0)
        {
            currentAttackCooldown -= deltaTime;
        }

        if (distanceToPlayer > attackRange)
        {
            desiredVelocity = playerDir * moveSpeed;
        }
        else
        {
            if (currentAttackCooldown <= 0)
            {
                Attack();
                currentAttackCooldown = attackCooltime;
            }
        }
    }

    protected virtual void Attack()
    {
        Debug.Log("기본 근접 몹 공격!");
    }

    public virtual void TakeDamage(float damage)
    {
        currentHealth -= damage;
        GameObject textObj = DamageTextPool.Instance.GetDamageText();
        textObj.GetComponent<DamageText>().Setup(damage, damageTextSpawnpoint.position);

        if (currentHealth <= 0)
        {

            currentHealth = 0;
            Die();
        }
    }

    public virtual void Die()
    {
        DropExpOrbs();

        RunManager.Instance.AddKill();

        if (myManager != null)
        {
            myManager.ReturnToPool(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void DropExpOrbs()
    {
        Vector3 dropPos = transform.position;
        dropPos.y += 0.05f;

        
        ExpType typeToDrop = ExpType.Small;
        if (expReward >= 50) typeToDrop = ExpType.Large;
        else if (expReward >= 10) typeToDrop = ExpType.Medium;

        if (ExpManager.Instance != null)
        {
            ExpManager.Instance.SpawnExp(typeToDrop, dropPos, expReward);
        }
        
    }
}