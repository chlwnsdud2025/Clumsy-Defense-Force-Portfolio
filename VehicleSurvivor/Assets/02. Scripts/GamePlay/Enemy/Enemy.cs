using UnityEngine;

public interface IDamageable
{
    void TakeDamage(DamageInfo damageInfo);
    float ContactDamage { get; }
}

public class Enemy : MonoBehaviour, IDamageable
{
    protected Transform playerTarget;
    protected EnemyManager myManager;
    protected IDamageable playerDamageable; // [수정] 자식 클래스에서 접근할 수 있도록 protected로 변경

    [Header("UI")]
    public Transform damageTextSpawnpoint;

    [HideInInspector] public EnemyPoolDataSO originPool;

    [Header("Stat")]
    public float moveSpeed = 5f;
    public float maxHealth = 10;
    protected float currentHealth;
    public float rotationSpeed = 2f;

    [Header("Exp & Money Reward")]
    public int expReward = 34;
    public int moneyReward = 15;

    [Header("Separation Settings")]
    public float separationRadius = 1.5f;
    public float separationWeight = 0.5f;

    // ==========================================
    // 💥 [핵심 분리 1] 몸통 충돌(Contact) 세팅
    // ==========================================
    [Header("Contact Damage (몸통 충돌)")]
    public float contactDamage = 10f;
    public float ContactDamage => contactDamage;

    [Tooltip("Y축을 포함하여 플레이어와 이 거리만큼 가까워지면 몸통 데미지를 입힙니다.")]
    public float contactRange = 1.5f;

    [Tooltip("연속으로 부딪혔을 때 데미지가 다단히트로 들어가는 것을 막는 쿨타임")]
    public float contactCooltime = 1.0f;
    protected float currentContactCooldown;

    // ==========================================
    // 🎯 [핵심 분리 2] 특수 패턴(Pattern) 세팅
    // ==========================================
    [Header("Pattern / Skill Behavior (특수/원거리 패턴)")]
    [Tooltip("원거리 몹이나 특수 패턴이 있는 몹이 멈춰서 공격을 시작할 3D 거리")]
    public float patternRange = 5.0f;

    [Tooltip("특수 패턴 재사용 대기시간")]
    public float patternCooltime = 3.0f;
    protected float currentPatternCooldown;

    [Header("3D Ground Snapping & Gravity")]
    public LayerMask groundLayer;
    public float raycastStartHeight = 50f;
    public float raycastDistance = 100f;
    public float fastStepSpeed = 25f;
    public float slowClimbSpeed = 5f;
    public float gravity = -20f;

    [SerializeField] private HitFlashEffect hitFlash;

    protected bool isTrackingPlayer = true;
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
        playerDamageable = target != null ? target.GetComponentInChildren<IDamageable>() : null;
        transform.position = spawnPosition;
        gameObject.SetActive(true);
        currentHealth = maxHealth;
        knockbackVelocity = Vector3.zero;

        // 쿨타임 초기화
        currentContactCooldown = 0f;
        currentPatternCooldown = 0f;

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

        // 1. [핵심 추가] 매 프레임 독립적으로 3D 몸통 충돌 체크!
        CheckContactDamage(deltaTime);

        // 2. 이동 및 패턴 AI 실행
        ExecuteBehavior(deltaTime, out Vector3 desiredVelocity);

        Vector3 separationForce = myManager.GetSeparationForce(this, separationRadius);
        separationForce.y = 0;
        Vector3 horizontalVelocity = desiredVelocity + (separationForce * separationWeight);

        // --- (이하 기존 지형 추적 및 중력 코드는 완벽히 동일하여 생략 없이 유지) ---
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
                else currentClimbSpeed = fastStepSpeed;

                float newY = Mathf.MoveTowards(transform.position.y, cachedTargetHeight, currentClimbSpeed * deltaTime);
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
                verticalVelocity = 0;
                isGrounded = true;
            }
            else isGrounded = false;
        }
        else isGrounded = false;

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

    // ==========================================
    // ⚔️ 독립된 3D 몸통 충돌 체크 함수
    // ==========================================
    protected virtual void CheckContactDamage(float deltaTime)
    {
        if (playerDamageable == null) return;

        if (currentContactCooldown > 0)
        {
            currentContactCooldown -= deltaTime;
        }

        // [수정] Y축 고도를 포함한 완벽한 3D 거리 계산 (공중몹이나 언덕 몹 대응)
        float distance3D = Vector3.Distance(transform.position, playerTarget.position);

        if (distance3D <= contactRange)
        {
            if (currentContactCooldown <= 0)
            {
                playerDamageable.TakeDamage(DamageInfo.Normal(contactDamage));
                currentContactCooldown = contactCooltime; // 쿨타임 돌입
            }
        }
    }

    // ==========================================
    // 🎯 특수 패턴(원거리/돌진 등) 실행 AI
    // ==========================================
    protected virtual void ExecuteBehavior(float deltaTime, out Vector3 desiredVelocity)
    {
        // 이동은 XZ 평면으로만 계산합니다.
        Vector3 playerDirXZ = playerTarget.position - transform.position;
        playerDirXZ.y = 0;
        playerDirXZ = playerDirXZ.normalized;

        desiredVelocity = Vector3.zero;

        if (currentPatternCooldown > 0)
        {
            currentPatternCooldown -= deltaTime;
        }

        // [수정] 패턴 발동 여부 역시 Y축을 포함한 3D 거리로 엄격하게 잽니다.
        float distanceToPlayer3D = Vector3.Distance(transform.position, playerTarget.position);

        // 플레이어가 패턴 사거리 밖에 있으면 다가갑니다.
        if (distanceToPlayer3D > patternRange)
        {
            desiredVelocity = playerDirXZ * moveSpeed;
        }
        else
        {
            // 패턴 사거리 안에 들어오면 정지하고 패턴 실행
            if (currentPatternCooldown <= 0)
            {
                Attack(); // 자식 클래스들이 override한 특수 패턴 실행
                currentPatternCooldown = patternCooltime;
            }
        }
    }

    protected virtual void Attack()
    {
        // 이제 Base Enemy의 Attack은 비워둡니다!
        // 1. 단순 근접 몹(BasicEnemy)은 Attack을 안 써도 CheckContactDamage()가 알아서 부딪히면 딜을 줍니다.
        // 2. 특수 몹(Ranged, Laser 등)은 이 함수를 override해서 장판을 깔거나 미사일을 쏩니다.
    }

    // --- (TakeDamage, Die 등 기타 함수 동일) ---
    public virtual void TakeDamage(DamageInfo damageInfo)
    {
        currentHealth -= damageInfo.Amount;
        SoundPlayback.PlaySfx(ESoundId.SFX_Enemy_Hit);
        GameObject textObj = DamageTextPool.Instance.GetDamageText();
        textObj.GetComponent<DamageText>().Setup(damageInfo.Amount, damageTextSpawnpoint.position, damageInfo.IsCritical);

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
        SoundPlayback.PlaySfx(ESoundId.SFX_Enemy_Death);
        DropExpOrbs();
        DropMoney();
        RunManager.Instance.AddKill();

        if (myManager != null) myManager.ReturnToPool(this);
        else gameObject.SetActive(false);
    }

    private void DropExpOrbs()
    {
        Vector3 dropPos = transform.position;
        dropPos.y += 0.05f;
        if (ExpManager.Instance != null) ExpManager.Instance.SpawnExp(dropPos, expReward);
    }

    private void DropMoney()
    {
        Vector3 dropPos = transform.position;
        dropPos.y += 0.5f;
        if (MoneyManager.Instance != null && playerTarget != null)
            MoneyManager.Instance.SpawnMoney(dropPos, moneyReward, playerTarget);
    }

    private void ResolveHitFlash()
    {
        if (hitFlash == null) hitFlash = gameObject.GetOrAddComponent<HitFlashEffect>();
    }
}