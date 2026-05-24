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

    [Header("Money Reward")]
    public int moneyReward = 15;

    [Header("Separation Settings")]
    public float separationRadius = 1.5f;
    public float separationWeight = 0.5f;

    [Header("Behavior Settings")]
    public float attackRange = 2.0f;
    public float attackCooltime = 2.0f;

    [Header("3D Ground Snapping & Gravity")]
    public LayerMask groundLayer;
    public float raycastStartHeight = 50f;
    public float raycastDistance = 100f;
    public float fastStepSpeed = 25f;
    public float slowClimbSpeed = 5f;

    [Header("Attack")]
    public float contactDamage = 10f;
    public float ContactDamage => contactDamage;

    public float gravity = -20f;

    [SerializeField] private HitFlashEffect hitFlash;

    protected bool isTrackingPlayer = true;
    protected float currentAttackCooldown;
    protected Vector3 knockbackVelocity;

    private float verticalVelocity;
    private bool isGrounded;

    private float groundCheckTimer = 0f;
    private float groundCheckInterval = 0.1f;
    private RaycastHit[] hitBuffer = new RaycastHit[10];
    private float cachedTargetHeight;
    private bool cachedHitGround;

    private void Awake()
    {
        ResolveHitFlash();
    }

    public virtual void SpawnSetup(Transform target, Vector3 spawnPosition, EnemyManager manager)
    {
        myManager = manager;
        playerTarget = target;
        transform.position = spawnPosition;
        gameObject.SetActive(true);
        currentHealth = maxHealth;
        knockbackVelocity = Vector3.zero;
        currentAttackCooldown = 0f;

        verticalVelocity = 0f;
        isGrounded = false;

        groundCheckTimer = Random.Range(0f, groundCheckInterval);
        cachedTargetHeight = spawnPosition.y;
        cachedHitGround = true;

        hitFlash?.ResetFlash();
    }

    public virtual void ManagedUpdate(float deltaTime)
    {
        if (playerTarget == null) return;

        ExecuteBehavior(deltaTime, out Vector3 desiredVelocity);

        Vector3 separationForce = myManager.GetSeparationForce(this, separationRadius);
        separationForce.y = 0;

        Vector3 horizontalVelocity = desiredVelocity + (separationForce * separationWeight);

        groundCheckTimer += deltaTime;

        if (groundCheckTimer >= groundCheckInterval)
        {
            groundCheckTimer = 0f;
            Vector3 checkPosXZ = transform.position + (horizontalVelocity * groundCheckInterval);
            cachedHitGround = false;
            cachedTargetHeight = transform.position.y;

            bool isPlayerHigher = playerTarget.position.y > transform.position.y + 2.0f;

            if (isPlayerHigher)
            {
                float dynamicHeight = Mathf.Max(transform.position.y + raycastStartHeight, playerTarget.position.y + 5.0f);
                Vector3 highRayStart = checkPosXZ + Vector3.up * dynamicHeight;
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
            else
            {
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

        if (cachedHitGround)
        {
            float heightDiff = cachedTargetHeight - transform.position.y;

            if (heightDiff >= -0.1f)
            {
                float currentClimbSpeed;
                if (heightDiff > 0.5f)
                {
                    horizontalVelocity = Vector3.zero;
                    currentClimbSpeed = slowClimbSpeed;
                }
                else
                {
                    currentClimbSpeed = fastStepSpeed;
                }

                float newY = Mathf.MoveTowards(transform.position.y, cachedTargetHeight, currentClimbSpeed * deltaTime);
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);

                verticalVelocity = 0;
                isGrounded = true;
            }
            else
            {
                isGrounded = false;
            }
        }
        else
        {
            isGrounded = false;
        }

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

        transform.position += (horizontalVelocity + Vector3.up * verticalVelocity) * deltaTime;

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

        hitFlash?.TriggerHitFlash();

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public virtual void Die()
    {
        hitFlash?.ResetFlash();
        DropExpOrbs();

        DropMoney();
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

        if (ExpManager.Instance != null)
        {
            ExpManager.Instance.SpawnExp(dropPos, expReward);
        }
    }

    private void DropMoney()
    {
        Vector3 dropPos = transform.position;
        dropPos.y += 0.5f;

        if (MoneyManager.Instance != null && playerTarget != null)
        {
            // 루프 없이 단 1개의 돈에 전체 금액을 담아서 드랍
            MoneyManager.Instance.SpawnMoney(dropPos, moneyReward, playerTarget);
        }
    }

    private void ResolveHitFlash()
    {
        if (hitFlash == null)
            hitFlash = gameObject.GetOrAddComponent<HitFlashEffect>();
    }
}
