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
        if (playerTarget == null) return;

        float dt = Time.deltaTime;

        CheckContactDamage(dt);

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

    private void MoveAndSnapToGround(float dt)
    {
        Vector3 playerDir = playerTarget.position - transform.position;
        playerDir.y = 0;
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


    public virtual void TakeDamage(float damage)
    {
        currentHealth -= damage;

        GameObject textObj = DamageTextPool.Instance.GetDamageText();
        textObj.GetComponent<DamageText>().Setup(damage, damageTextSpawnpoint.position);

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
                    player.TakeDamage(contactDamage);
                    currentContactCooldown = contactCooltime;
                }
            }
        }
    }
}
