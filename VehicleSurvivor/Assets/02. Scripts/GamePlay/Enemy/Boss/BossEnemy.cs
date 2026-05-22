using UnityEngine;

// 잡몹(Enemy)과는 완전히 분리된, 보스들만의 새로운 최상위 부모 클래스!
public abstract class BossEnemy : MonoBehaviour, IDamageable
{
    [Header("Base References")]
    public Transform playerTarget;

    // [신규 추가] 보스도 데미지 텍스트를 띄울 위치가 필요합니다!
    [Header("UI")]
    public Transform damageTextSpawnpoint;

    [Header("Base Stats")]
    public float maxHealth = 10f;
    protected float currentHealth;
    public float moveSpeed = 3f;
    public float rotationSpeed = 3f;

    [Header("Rewards")]
    public int expReward = 500;   // 보스용 대량 경험치
    public int moneyReward = 300; // 보스용 대량 골드

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
    // [최적화됨] 피격 점멸 (Hit Flash) 설정 (Enemy와 동일)
    // ==========================================
    [Header("Hit Flash Settings (Optimized)")]
    public Renderer[] modelRenderers;
    public float flashDuration = 0.1f;
    public float flashCooldown = 0.1f;
    public string colorPropertyName = "_Color"; // URP라면 "_BaseColor" 로 변경
    public float flashIntensity = 5f;
    [Range(0f, 1f)] public float flashAlpha = 0.4f;

    private Color[] originalColors;
    private Color[] precalculatedFlashColors;
    private int colorPropertyID;

    private MaterialPropertyBlock propBlock;
    private float flashTimer = 0f;
    private float flashCooldownTimer = 0f;
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

        // [최적화 적용] 몹이 태어날 때 미리 색상과 ID를 계산해 둡니다.
        propBlock = new MaterialPropertyBlock();
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

                    precalculatedFlashColors[i] = Color.Lerp(originalColors[i], Color.white * flashIntensity, flashAlpha);
                }
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

        // [신규 적용] 보스도 데미지 텍스트를 띄웁니다!
        if (damageTextSpawnpoint != null && DamageTextPool.Instance != null)
        {
            GameObject textObj = DamageTextPool.Instance.GetDamageText();
            textObj.GetComponent<DamageText>().Setup(damage, damageTextSpawnpoint.position);
        }

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

        DropExpOrbs(); // [신규] 보스 경험치 드랍
        DropMoney();   // [신규] 보스 재화 드랍


        if (StageRunManager.Instance != null)
        {
            StageRunManager.Instance.OnMidBossDefeated();
        }
        Destroy(gameObject);
    }

    // 피격 점멸 적용 로직 (Enemy.cs와 동일)
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

            ApplyFlashColor(precalculatedFlashColors);

            if (flashTimer <= 0f)
            {
                RestoreOriginalColors();
            }
        }
    }

    private void ApplyFlashColor(Color[] colorsToApply)
    {
        if (modelRenderers == null || colorsToApply == null) return;

        for (int i = 0; i < modelRenderers.Length; i++)
        {
            if (modelRenderers[i] != null)
            {
                modelRenderers[i].GetPropertyBlock(propBlock);
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


    private void DropExpOrbs()
    {
        Vector3 dropPos = transform.position;
        dropPos.y += 0.5f;

        if (ExpManager.Instance != null)
        {
            // 보스의 거대한 경험치(예: 500)를 던져주면, 
            // 매니저가 알아서 "앗 50 이상이네! 제일 큰 구슬(Large) 줘야지!" 하고 처리합니다.
            ExpManager.Instance.SpawnExp(dropPos, expReward);
        }
    }

    private void DropMoney()
    {
        Vector3 dropPos = transform.position;
        dropPos.y += 0.5f;

        if (MoneyManager.Instance != null && playerTarget != null)
        {
            // 돈도 여러 개가 흩뿌려지도록 3개 정도로 분할해서 드랍
            for (int i = 0; i < 3; i++)
            {
                MoneyManager.Instance.SpawnMoney(dropPos, moneyReward / 3, playerTarget);
            }
        }
    }
}