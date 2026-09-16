using System.Collections.Generic;
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

    // 회수 규칙(EnemyManager.CheckRecycle) 추적값. 스폰 시 초기화.
    [System.NonSerialized] public Vector3 recycleLastPos;
    [System.NonSerialized] public float recycleStuckTime;

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

    [Header("Overlap Block")]
    public float playerBlockRadius = 0.8f;
    public float playerBlockHeight = 2.0f;

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
    [Header("Pattern / Weapon Behavior (특수/원거리 패턴)")]
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

    private float verticalVelocity;
    private bool isGrounded;
    private float groundCheckTimer = 0f;
    private float groundCheckInterval = 0.1f;
    private RaycastHit[] hitBuffer = new RaycastHit[10];
    private float cachedTargetHeight;
    private bool cachedHitGround;


    protected bool isDead = false;

#if UNITY_EDITOR
    // ==========================================
    // [에디터 전용] 다층 추적(2단 레이캐스트) 시각화용 스냅샷
    // Editor/EnemyFloorTrackingGizmo 가 읽습니다. 게임 로직에는 영향 없음.
    // ==========================================
    public enum EFloorTrackMode { None, PlayerHigher, LowRay, LowRayMissFallback }
    public enum EFloorMoveState { None, Grounded, FastStep, SlowClimb, Falling }

    public struct FloorTrackSnapshot
    {
        public bool valid;
        public EFloorTrackMode mode;
        public Vector3 checkPos;          // 예측 위치 (현재 위치 + 수평속도 × interval)
        public Vector3 lowRayStart;
        public float lowRayDistance;
        public bool lowRayHit;
        public Vector3 lowHitPoint;
        public Vector3 highRayStart;
        public float highRayDistance;
        public int highHitCount;
        public Vector3[] highHits;
        public int chosenHitIndex;
        public float playerY;
        public float targetHeight;
        public bool hitGround;
        public float heightDiff;
        public EFloorMoveState moveState;
    }

    public FloorTrackSnapshot FloorDebug;
    public readonly List<Vector3> FloorDebugHistory = new List<Vector3>();
    public const int FloorDebugHistoryMax = 80;

    private void RecordHighHits(int hitCount)
    {
        if (FloorDebug.highHits == null) FloorDebug.highHits = new Vector3[hitBuffer.Length];
        FloorDebug.highHitCount = hitCount;
        FloorDebug.chosenHitIndex = -1;
        float closest = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            FloorDebug.highHits[i] = hitBuffer[i].point;
            float diff = Mathf.Abs(hitBuffer[i].point.y - playerTarget.position.y);
            if (diff < closest) { closest = diff; FloorDebug.chosenHitIndex = i; }
        }
    }
#endif

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
        isDead = false;


        // 쿨타임 초기화
        currentContactCooldown = 0f;
        currentPatternCooldown = 0f;

        verticalVelocity = 0f;
        isGrounded = false;
        groundCheckTimer = Random.Range(0f, groundCheckInterval);
        cachedTargetHeight = spawnPosition.y;
        cachedHitGround = true;

        recycleLastPos = spawnPosition;
        recycleStuckTime = 0f;

#if UNITY_EDITOR
        FloorDebug.valid = false;
        FloorDebugHistory.Clear();
#endif

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
        ClipPlayerOverlap(ref horizontalVelocity);

        // --- (이하 기존 지형 추적 및 중력 코드는 완벽히 동일하여 생략 없이 유지) ---
        groundCheckTimer += deltaTime;
        if (groundCheckTimer >= groundCheckInterval)
        {
            groundCheckTimer = 0f;
            Vector3 checkPosXZ = transform.position + (horizontalVelocity * groundCheckInterval);
            cachedHitGround = false;
            cachedTargetHeight = transform.position.y;

            bool isPlayerHigher = playerTarget.position.y > transform.position.y + 2.0f;

#if UNITY_EDITOR
            FloorDebug.valid = true;
            FloorDebug.checkPos = checkPosXZ;
            FloorDebug.playerY = playerTarget.position.y;
            FloorDebug.lowRayHit = false;
            FloorDebug.highHitCount = 0;
            FloorDebug.chosenHitIndex = -1;
            FloorDebug.mode = isPlayerHigher ? EFloorTrackMode.PlayerHigher : EFloorTrackMode.LowRay;
#endif

            if (isPlayerHigher)
            {
                float dynamicHeight = Mathf.Max(transform.position.y + raycastStartHeight, playerTarget.position.y + 5.0f);
                Vector3 highRayStart = checkPosXZ + Vector3.up * dynamicHeight;
                int hitCount = Physics.RaycastNonAlloc(highRayStart, Vector3.down, hitBuffer, raycastDistance, groundLayer);

#if UNITY_EDITOR
                FloorDebug.highRayStart = highRayStart;
                FloorDebug.highRayDistance = raycastDistance;
                RecordHighHits(hitCount);
#endif

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

#if UNITY_EDITOR
                FloorDebug.lowRayStart = lowRayStart;
                FloorDebug.lowRayDistance = lowRayDistance;
#endif

                if (Physics.Raycast(lowRayStart, Vector3.down, out RaycastHit lowHit, lowRayDistance, groundLayer))
                {
                    cachedTargetHeight = lowHit.point.y;
                    cachedHitGround = true;
#if UNITY_EDITOR
                    FloorDebug.lowRayHit = true;
                    FloorDebug.lowHitPoint = lowHit.point;
#endif
                }
                else
                {
                    Vector3 highRayStart = checkPosXZ + Vector3.up * raycastStartHeight;
                    int hitCount = Physics.RaycastNonAlloc(highRayStart, Vector3.down, hitBuffer, raycastDistance, groundLayer);

#if UNITY_EDITOR
                    FloorDebug.mode = EFloorTrackMode.LowRayMissFallback;
                    FloorDebug.highRayStart = highRayStart;
                    FloorDebug.highRayDistance = raycastDistance;
                    RecordHighHits(hitCount);
#endif

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

#if UNITY_EDITOR
            FloorDebug.hitGround = cachedHitGround;
            FloorDebug.targetHeight = cachedTargetHeight;
            if (cachedHitGround)
            {
                FloorDebugHistory.Add(new Vector3(checkPosXZ.x, cachedTargetHeight, checkPosXZ.z));
                if (FloorDebugHistory.Count > FloorDebugHistoryMax) FloorDebugHistory.RemoveAt(0);
            }
#endif
        }

        if (cachedHitGround)
        {
            float heightDiff = cachedTargetHeight - transform.position.y;
#if UNITY_EDITOR
            FloorDebug.heightDiff = heightDiff;
            FloorDebug.moveState = heightDiff < -0.1f ? EFloorMoveState.Falling
                : heightDiff > 0.5f ? EFloorMoveState.SlowClimb
                : Mathf.Abs(heightDiff) > 0.01f ? EFloorMoveState.FastStep
                : EFloorMoveState.Grounded;
#endif
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
        else
        {
            isGrounded = false;
#if UNITY_EDITOR
            FloorDebug.moveState = EFloorMoveState.Falling;
#endif
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

    protected void ClipPlayerOverlap(ref Vector3 horizontalVelocity)
    {
        if (playerTarget == null) return;

        float playerRadius = myManager != null ? myManager.PlayerBlockRadius : EnemyManager.DefaultPlayerBlockRadius;

        // 업그레이드된 유틸리티 함수에 playerBlockHeight를 함께 전달합니다.
        OverlapBlockUtility.ClipApproach(
            ref horizontalVelocity,
            transform.position,
            playerTarget.position,
            playerBlockRadius + playerRadius,
            playerBlockHeight);
    }

    // --- (TakeDamage, Die 등 기타 함수 동일) ---
    public virtual void TakeDamage(DamageInfo damageInfo)
    {

        if (isDead) return;


        DamageInfo finalDamageInfo = ItemSpecialEffectService.ApplyOnEnemyHit(this, damageInfo);
        currentHealth -= finalDamageInfo.Amount;
        SoundPlayback.PlaySfx(ESoundId.SFX_Enemy_Hit);
        GameObject textObj = DamageTextPool.Instance.GetDamageText();
        textObj.GetComponent<DamageText>().Setup(finalDamageInfo.Amount, damageTextSpawnpoint.position, finalDamageInfo.IsCritical);

        hitFlash?.TriggerHitFlash();

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public virtual void Die()
    {
        if (isDead) return;

        isDead = true;
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


#if UNITY_EDITOR
    // Tools > Enemy Spatial Grid Gizmo 메뉴에서 토글 (공간분할 기즈모 캡처 시 끌 수 있음)
    public static bool DrawDefaultGizmos = true;

    private void OnDrawGizmosSelected()
    {
        if (!DrawDefaultGizmos) return;

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
