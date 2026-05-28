using UnityEngine;

// 잡몹(Enemy)과는 완전히 분리된, 보스들만의 새로운 최상위 부모 클래스!
public abstract class BossEnemy : MonoBehaviour, IDamageable
{


    [Header("Base References")]
    public Transform playerTarget;

    [Header("UI")]
    public Transform damageTextSpawnpoint;
    [SerializeField] private string displayName;

    [Header("Base Stats")]
    public float maxHealth = 10f;
    protected float currentHealth;
    public float moveSpeed = 3f;
    public float rotationSpeed = 3f;

    [Header("Contact Damage (보스 몸통 충돌)")]
    public float contactRange = 2.0f; // 보스는 덩치가 크니 범위를 좀 더 넓게 잡는 게 좋습니다.
    public float contactCooltime = 1.0f;
    protected float currentContactCooldown;

    [Header("Rewards")]
    public int expReward = 10;
    public int moneyReward = 10;

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

    [Header("Overlap Block (보스 통과 방지)")]
    public float playerBlockRadius = 1.5f; // 보스 덩치에 맞게 반경을 넓게 설정
    public float playerBlockHeight = 4.0f;

    private float verticalVelocity;
    private bool isGrounded;

    [Header("Combat Base")]
    public float attackCooltime = 4.0f;
    protected float currentAttackCooldown;

    private RaycastHit[] hitBuffer = new RaycastHit[10];
    protected bool isCastingPattern = false;

    [SerializeField] private HitFlashEffect hitFlash;

    private IEventBus _eventBus;
    private bool _defeatedPublished;
    private EntityId BossId => gameObject.GetEntityId();

    protected virtual void Awake()
    {
        ResolveHitFlash();
        ServiceLocator.TryGet(out _eventBus);
    }

    protected virtual void OnEnable()
    {
        BlockableRegistry.Register(transform, playerBlockRadius);
    }

    protected virtual void OnDisable()
    {
        BlockableRegistry.Unregister(transform);
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        currentAttackCooldown = attackCooltime;

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }

        PublishAppeared();
    }

    private void OnDestroy()
    {
        if (!_defeatedPublished)
            PublishDefeated();
    }

    protected virtual void Update()
    {
        if (playerTarget == null)
            return;

        if (RunManager.TryGetInstance(out RunManager runManager) &&
            runManager.RunSession != null &&
            !runManager.RunSession.ShouldUpdateGameplay)
            return;

        float dt = Time.deltaTime;

        CheckContactDamage(dt);

        if (!isCastingPattern)
        {
            currentAttackCooldown -= dt;
            if (currentAttackCooldown <= 0)
            {
                Attack();
                SoundPlayback.PlaySfx(ESoundId.SFX_Boss_Attack);
            }
        }

        MoveAndSnapToGround(dt);
    }

    protected abstract void Attack();

    private void MoveAndSnapToGround(float dt)
    {
        Vector3 playerDir = playerTarget.position - transform.position;
        playerDir.y = 0;
        playerDir = playerDir.normalized;

        Vector3 horizontalVelocity = playerDir * moveSpeed;
        ClipPlayerOverlap(ref horizontalVelocity);

        Vector3 nextPosXZ = transform.position + (horizontalVelocity * dt);
        float targetHeight = transform.position.y;
        bool hitGround = false;

        bool isPlayerHigher = playerTarget.position.y > transform.position.y + 2.0f;
        if (isPlayerHigher)
        {
            float dynamicHeight = Mathf.Max(transform.position.y + raycastStartHeight, playerTarget.position.y + 5.0f);
            Vector3 highRayStart = nextPosXZ + Vector3.up * dynamicHeight;

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


    public virtual void TakeDamage(DamageInfo damageInfo)
    {
        DamageInfo finalDamageInfo = ItemSpecialEffectService.ApplyOnEnemyHit(this, damageInfo);
        currentHealth -= finalDamageInfo.Amount;

        GameObject textObj = DamageTextPool.Instance.GetDamageText();
        textObj.GetComponent<DamageText>().Setup(finalDamageInfo.Amount, damageTextSpawnpoint.position, finalDamageInfo.IsCritical);

        hitFlash?.TriggerHitFlash();
        PublishHpChanged();

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    protected virtual void Die()
    {
        hitFlash?.ResetFlash();
        Debug.Log($"{gameObject.name} 보스 처치 완료!");

        PublishDefeated();

        DropExpOrbs();
        DropMoney();

        HandleStageClear();

        Destroy(gameObject);
    }

    private string GetDisplayName()
    {
        return string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    }

    private void PublishAppeared()
    {
        SoundPlayback.PlaySfx(ESoundId.SFX_Boss_Appear);

        if (_eventBus == null && !ServiceLocator.TryGet(out _eventBus))
            return;

        int current = Mathf.RoundToInt(currentHealth);
        int max = Mathf.RoundToInt(maxHealth);
        _eventBus.Publish(new BossAppearedEvent(BossId, GetDisplayName(), current, max));
    }

    private void PublishHpChanged()
    {
        if (_eventBus == null && !ServiceLocator.TryGet(out _eventBus))
            return;

        int current = Mathf.RoundToInt(currentHealth);
        int max = Mathf.RoundToInt(maxHealth);
        _eventBus.Publish(new BossHpChangedEvent(BossId, current, max));
    }

    private void PublishDefeated()
    {
        if (_defeatedPublished)
            return;

        _defeatedPublished = true;

        SoundPlayback.PlaySfx(ESoundId.SFX_Boss_Defeat);

        if (_eventBus == null && !ServiceLocator.TryGet(out _eventBus))
            return;

        _eventBus.Publish(new BossDefeatedEvent(BossId));
    }

    protected virtual void HandleStageClear()
    {
        StageRunManager.Instance.OnMidBossDefeated();
    }

    private void ResolveHitFlash()
    {
        if (hitFlash == null)
            hitFlash = gameObject.GetOrAddComponent<HitFlashEffect>();
    }
    private void DropExpOrbs()
    {
        Vector3 dropPos = transform.position;
        dropPos.y += 0.5f;

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
            MoneyManager.Instance.SpawnMoney(dropPos, moneyReward, playerTarget);

    }

    protected virtual void CheckContactDamage(float deltaTime)
    {
        if (currentContactCooldown > 0)
        {
            currentContactCooldown -= deltaTime;
        }

        // 보스는 IDamageable을 상속받는 Player를 타겟으로 함
        IDamageable player = playerTarget.GetComponent<IDamageable>();

        if (player != null)
        {
            float distance3D = Vector3.Distance(transform.position, playerTarget.position);

            if (distance3D <= contactRange)
            {
                if (currentContactCooldown <= 0)
                {
                    player.TakeDamage(DamageInfo.Normal(contactDamage));
                    currentContactCooldown = contactCooltime;
                }
            }
        }
    }

    private void ClipPlayerOverlap(ref Vector3 horizontalVelocity)
    {
        if (playerTarget == null)
            return;

        // ==========================================
        // 🚀 [핵심 추가] 보스와 플레이어의 고도(Y축) 차이 계산
        // ==========================================
        float yDiff = Mathf.Abs(playerTarget.position.y - transform.position.y);

        // 플레이어가 보스의 키(playerBlockHeight)보다 높이 점프했다면 블록 무시!
        if (yDiff >= playerBlockHeight)
        {
            return; // 겹침 방지 로직을 건너뛰고 그대로 통과시킴
        }

        // ==========================================
        float playerRadius = EnemyManager.Instance != null
            ? EnemyManager.Instance.PlayerBlockRadius
            : EnemyManager.DefaultPlayerBlockRadius;

        OverlapBlockUtility.ClipApproach(
            ref horizontalVelocity,
            transform.position,
            playerTarget.position,
            playerBlockRadius + playerRadius);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 1. 통과 방지(Overlap Block) 범위 - 빨간색 원기둥 형태
        Gizmos.color = new Color(1f, 0f, 0f, 0.7f); // 반투명한 빨간색

        Vector3 basePos = transform.position;
        Vector3 topPos = basePos + (Vector3.up * playerBlockHeight);

        // 아래 바닥과 위쪽 천장을 보여주는 원(구체)
        Gizmos.DrawWireSphere(basePos, playerBlockRadius);
        Gizmos.DrawWireSphere(topPos, playerBlockRadius);

        // 원기둥의 형태를 잡아주는 4개의 기둥 선
        Gizmos.DrawLine(basePos + Vector3.forward * playerBlockRadius, topPos + Vector3.forward * playerBlockRadius);
        Gizmos.DrawLine(basePos - Vector3.forward * playerBlockRadius, topPos - Vector3.forward * playerBlockRadius);
        Gizmos.DrawLine(basePos + Vector3.right * playerBlockRadius, topPos + Vector3.right * playerBlockRadius);
        Gizmos.DrawLine(basePos - Vector3.right * playerBlockRadius, topPos - Vector3.right * playerBlockRadius);

        // 2. 몸통 충돌(Contact Damage) 판정 범위 - 노란색 구체
        // (보스 스크립트인 경우 아래 변수가 ContactRange일 수 있으니 변수명에 맞게 조절하세요)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, contactRange);
    }
#endif
}
