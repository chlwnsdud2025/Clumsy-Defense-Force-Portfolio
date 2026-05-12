using UnityEngine;

public enum EnemyType
{
    Normal_A,
    Normal_B,
    Normal_C,
    Ranged,
    Dash,
    Suicide,
    Flying_A,
    Raser
}

public interface IDamageable
{
    void TakeDamage(float damage);
}

public class Enemy : MonoBehaviour, IDamageable
{
    protected Transform playerTarget;
    protected EnemyManager myManager;

    public EnemyType myType;

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

    public float gravity = -20f; // 증력

    protected bool isTrackingPlayer = true;

    protected float currentAttackCooldown;
    protected Vector3 knockbackVelocity;

    // 수직 이동(점프/추락) 처리를 위한 상태 변수
    private float verticalVelocity;
    private bool isGrounded;

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
        // [완성판] 다중 레이어 투시 스마트 레이캐스트
        // ==========================================
        Vector3 nextPosXZ = transform.position + (horizontalVelocity * deltaTime);
        float targetHeight = transform.position.y;
        bool hitGround = false;

        // 1. 플레이어가 나보다 높은 곳(예: 2m 이상)에 있는지 확인
        bool isPlayerHigher = playerTarget.position.y > transform.position.y + 2.0f;

        if (isPlayerHigher)
        {
            // [플레이어가 높은 곳에 있음]
            // RaycastAll을 사용하여 하늘에서부터 모든 지형을 관통하여 스캔합니다.
            float dynamicHeight = Mathf.Max(transform.position.y + raycastStartHeight, playerTarget.position.y + 5.0f);
            Vector3 highRayStart = nextPosXZ + Vector3.up * dynamicHeight;
            RaycastHit[] hits = Physics.RaycastAll(highRayStart, Vector3.down, raycastDistance, groundLayer);

            if (hits.Length > 0)
            {
                float closestDiff = float.MaxValue;
                for (int i = 0; i < hits.Length; i++)
                {
                    // 플레이어의 높이와 가장 가까운 지형 선택
                    float diff = Mathf.Abs(hits[i].point.y - playerTarget.position.y);
                    if (diff < closestDiff)
                    {
                        closestDiff = diff;
                        targetHeight = hits[i].point.y;
                        hitGround = true;
                    }
                }
            }
        }
        else
        {
            // [플레이어가 평지이거나 내 아래에 있음]
            // 다리 밑 천장에 납치되지 않도록 근거리(머리 위 2m) 레이더부터 가동
            float lowRayStartHeight = 2.0f;
            float lowRayDistance = 4.0f;
            Vector3 lowRayStart = nextPosXZ + Vector3.up * lowRayStartHeight;

            if (Physics.Raycast(lowRayStart, Vector3.down, out RaycastHit lowHit, lowRayDistance, groundLayer))
            {
                targetHeight = lowHit.point.y;
                hitGround = true;
            }
            // 근거리 탐색 실패 시 (절벽 등반 혹은 다층 지형 탐색)
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
                        if (diff < closestDiff)
                        {
                            closestDiff = diff;
                            targetHeight = hits[i].point.y;
                            hitGround = true;
                        }
                    }
                }
            }
        }


        // 지형 충돌 결과 처리(단차)
        if (hitGround)
        {
            float heightDiff = targetHeight - transform.position.y;

            if (heightDiff >= -0.1f)
            {
                float currentClimbSpeed;

                // '실제 단차의 높이'에 따라 등반/보행 속도 결정
                if (heightDiff > 0.5f)
                {
                    // 거대한 벽을 만나면 전진을 멈추고 천천히 등반
                    horizontalVelocity = Vector3.zero;
                    currentClimbSpeed = slowClimbSpeed;
                }
                else
                {
                    // 작은 단차는 멈추지 않고 빠른 속도로 밟고 지나감
                    currentClimbSpeed = fastStepSpeed;
                }

                float newY = Mathf.MoveTowards(transform.position.y, targetHeight, currentClimbSpeed * deltaTime);
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);

                verticalVelocity = 0;
                isGrounded = true;
            }
            else // 낭떠러지 등 하강 상황
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

            if (hitGround && transform.position.y + (verticalVelocity * deltaTime) < targetHeight)
            {
                transform.position = new Vector3(transform.position.x, targetHeight, transform.position.z);
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
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public virtual void Die()
    {
        DropExpOrbs();
        GameSessionManager.Instance.AddKill();

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
        dropPos.y += 0.5f;

        ExpType typeToDrop = ExpType.Small;
        if (expReward >= 50) typeToDrop = ExpType.Large;
        else if (expReward >= 10) typeToDrop = ExpType.Medium;

        if (ExpManager.Instance != null)
        {
            ExpManager.Instance.SpawnExp(typeToDrop, dropPos, expReward);
        }
    }
}