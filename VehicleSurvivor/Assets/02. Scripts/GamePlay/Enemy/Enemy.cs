using UnityEngine;
using System.Collections; // [추가] 코루틴 사용을 위해 필요

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
    public LayerMask groundLayer;
    public float raycastStartHeight = 50f;
    public float raycastDistance = 100f;
    public float fastStepSpeed = 25f;
    public float slowClimbSpeed = 5f;

    [Header("Attack")]
    public float contactDamage = 10f;
    public float ContactDamage => contactDamage;

    public float gravity = -20f;

    //coin
    [Header("CoinAmount")]
    public int coinAmount;


    //  피격 점멸 (Hit Flash) 설정
    [Header("Hit Flash Settings (Optimized)")]
    public Renderer[] modelRenderers;
    public float flashDuration = 0.1f;
    public float flashCooldown = 0.1f;
    public string colorPropertyName = "_Color";
    public float flashIntensity = 5f;
    [Range(0f, 1f)] public float flashAlpha = 0.4f;

    private Color[] originalColors;
    private Color[] precalculatedFlashColors; // [핵심] 섞인 하얀색을 미리 계산해서 담아둘 배열
    private int colorPropertyID;              // [핵심] 문자열 대신 쓸 고유 ID 번호

    private MaterialPropertyBlock propBlock;
    private float flashTimer = 0f;
    private float flashCooldownTimer = 0f;
    // ==========================================

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

    // [신규] 맨 처음 태어날 때 원래 색상을 기억해 둡니다.
    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();

        // [최적화 1] 문자열("_Color")을 유니티 엔진이 제일 빠르게 찾는 정수형 ID로 미리 변환합니다!
        colorPropertyID = Shader.PropertyToID(colorPropertyName);

        if (modelRenderers != null && modelRenderers.Length > 0)
        {
            originalColors = new Color[modelRenderers.Length];
            precalculatedFlashColors = new Color[modelRenderers.Length];

            for (int i = 0; i < modelRenderers.Length; i++)
            {
                if (modelRenderers[i] != null && modelRenderers[i].sharedMaterial != null)
                {
                    if (modelRenderers[i].sharedMaterial.HasProperty(colorPropertyID))
                        originalColors[i] = modelRenderers[i].sharedMaterial.GetColor(colorPropertyID);
                    else
                        originalColors[i] = Color.white;

                    // [최적화 2] 몹이 태어날 때 '원래색 + 하얀색 * 알파' 수학 연산을 아예 미리 다 해둡니다!
                    precalculatedFlashColors[i] = Color.Lerp(originalColors[i], Color.white * flashIntensity, flashAlpha);
                }
            }
        }
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

        
    }

    // (ManagedUpdate, ExecuteBehavior, Attack 코드는 이전과 완전히 동일하여 생략 없이 유지)
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

    // ==========================================
    // [수정] 피격 시 점멸 함수 호출
    // ==========================================
    public virtual void TakeDamage(float damage)
    {
        currentHealth -= damage;
        GameObject textObj = DamageTextPool.Instance.GetDamageText();
        textObj.GetComponent<DamageText>().Setup(damage, damageTextSpawnpoint.position);

        // 맞을 때마다 점멸 시도!
        TriggerHitFlash();

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public virtual void Die()
    {
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

    private void DropMoney()
    {
        Vector3 dropPos = transform.position;
        dropPos.y += 0.5f; // 바닥보다 살짝 위에서 튀어나오게

        if (MoneyManager.Instance != null && playerTarget != null)
        {
            // 몹이 죽을 때 매니저에게 돈을 스폰하라고 명령합니다.
            MoneyManager.Instance.SpawnMoney(dropPos, coinAmount, playerTarget);
        }
    }

    private void DropExpOrbs()
    {
        Vector3 dropPos = transform.position;
        dropPos.y += 0.05f;

        if (ExpManager.Instance != null)
        {
            // [최적화 완료] 복잡한 if문들이 싹 사라졌습니다!
            ExpManager.Instance.SpawnExp(dropPos, expReward);
        }
    }

    protected void TriggerHitFlash()
    {
        if (flashCooldownTimer <= 0f)
        {
            flashTimer = flashDuration;
            flashCooldownTimer = flashDuration + flashCooldown;
        }
    }

    protected virtual void LateUpdate()
    {
        if (flashCooldownTimer > 0f) flashCooldownTimer -= Time.deltaTime;

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;

            // 더 이상 여기서 수학 연산을 하지 않고, 함수 호출만 합니다.
            ApplyFlashColor(precalculatedFlashColors);

            if (flashTimer <= 0f)
            {
                RestoreOriginalColors();
            }
        }
    }

    // [최적화 3] 매개변수를 precalculatedFlashColors 배열 통째로 받습니다.
    private void ApplyFlashColor(Color[] colorsToApply)
    {
        if (modelRenderers == null || colorsToApply == null) return;

        for (int i = 0; i < modelRenderers.Length; i++)
        {
            if (modelRenderers[i] != null)
            {
                modelRenderers[i].GetPropertyBlock(propBlock);
                // 문자열 대신 colorPropertyID 사용, 수학 연산 없이 미리 계산된 색상 바로 투입!
                propBlock.SetColor(colorPropertyID, colorsToApply[i]);
                modelRenderers[i].SetPropertyBlock(propBlock);
            }
        }
    }

    private void RestoreOriginalColors()
    {
        if (modelRenderers == null || originalColors == null) return;

        for (int i = 0; i < modelRenderers.Length; i++)
        {
            if (modelRenderers[i] != null)
            {
                modelRenderers[i].GetPropertyBlock(propBlock);
                propBlock.SetColor(colorPropertyID, originalColors[i]);
                modelRenderers[i].SetPropertyBlock(propBlock);
            }
        }
    }
}