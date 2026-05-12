using UnityEngine;

public enum EnemyType
{
    Normal_A,
    Normal_B,
    Normal_C,
    Ranged,
    Dash,
    Suicide
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
    public float climbSmoothness = 15f;    // 절벽/경사면을 타고 오르는 속도
    public float gravity = -20f;           // 중력 가속도

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

    public void ManagedUpdate(float deltaTime)
    {
        if (playerTarget == null) return;

        // 수평 이동 방향 및 속도 계산 (X, Z)
        ExecuteBehavior(deltaTime, out Vector3 desiredVelocity);

        Vector3 separationForce = myManager.GetSeparationForce(this, separationRadius);
        separationForce.y = 0;

        Vector3 horizontalVelocity = desiredVelocity + (separationForce * separationWeight);

        if (knockbackVelocity.sqrMagnitude > 0.01f)
        {
            horizontalVelocity += knockbackVelocity;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, deltaTime * 5f);
        }

        // 레이캐스트를 통한 3D 지형 감지 및 등반
        Vector3 nextPosXZ = transform.position + (horizontalVelocity * deltaTime);
        float targetHeight = transform.position.y;

        // 몹의 다음 XZ 위치 기준, 상공에서 아래로 레이를 쏩니다.
        bool hitGround = Physics.Raycast(nextPosXZ + Vector3.up * raycastStartHeight, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer);

        if (hitGround)
        {
            targetHeight = hit.point.y;
            float heightDiff = targetHeight - transform.position.y;

            // 단차(maxStepHeight) 제한을 없앴습니다. 수직 절벽이든 평지이든 무조건 타고 오릅니다.
            if (heightDiff >= -0.1f)
            {
                // Lerp를 통해 벽을 스르륵 타고 오르는 연출
                float smoothY = Mathf.Lerp(transform.position.y, targetHeight, deltaTime * climbSmoothness);
                transform.position = new Vector3(transform.position.x, smoothY, transform.position.z);

                verticalVelocity = 0; // 지면에 붙어있으므로 수직 속도 초기화
                isGrounded = true;
            }
            else // 낭떠러지, 내리막길 등 현재 위치보다 지형이 한참 아래일 때
            {
                isGrounded = false;
            }
        }
        else
        {
            isGrounded = false; // 지형을 아예 찾지 못해도 추락
        }

        // 중력 적용 (공중에 떠 있거나 떨어질 때)
        if (!isGrounded)
        {
            verticalVelocity += gravity * deltaTime;

            // 추락 중 바닥을 뚫고 지나가지 않도록 최소 높이 보정
            if (hitGround && transform.position.y + (verticalVelocity * deltaTime) < targetHeight)
            {
                transform.position = new Vector3(transform.position.x, targetHeight, transform.position.z);
                verticalVelocity = 0;
                isGrounded = true;
            }
        }

        // 최종 위치 적용 (수평 이동 + 수직 이동)
        transform.position += (horizontalVelocity + Vector3.up * verticalVelocity) * deltaTime;

        // 회전 처리 (기존과 동일하게 Y축 무시)
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

    // 이동 방향과 공격 타이밍을 결정하는 부모의 공통 로직
    protected virtual void ExecuteBehavior(float deltaTime, out Vector3 desiredVelocity)
    {
        Vector3 playerDir = playerTarget.position - transform.position;
        playerDir.y = 0;
        float distanceToPlayer = playerDir.magnitude;
        playerDir = playerDir.normalized;

        desiredVelocity = Vector3.zero;

        // 매 프레임 쿨타임 감소
        if (currentAttackCooldown > 0)
        {
            currentAttackCooldown -= deltaTime;
        }

        // 사거리 밖이면 다가감
        if (distanceToPlayer > attackRange)
        {
            desiredVelocity = playerDir * moveSpeed;
        }
        else
        {
            // 사거리 이내이고 쿨타임이 돌았다면 공격
            if (currentAttackCooldown <= 0)
            {
                Attack();
                currentAttackCooldown = attackCooltime; // 쿨타임 리셋
            }
        }
    }

    protected virtual void Attack()
    {
        Debug.Log("기본 근접 몹이 차를 들이받습니다!");
    }

    public void ApplyKnockback(Vector3 pushDirection, float pushPower)
    {
        pushDirection.y = 0;
        knockbackVelocity = pushDirection.normalized * pushPower;
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

        if (expReward >= 50)
        {
            typeToDrop = ExpType.Large;
        }
        else if (expReward >= 10)
        {
            typeToDrop = ExpType.Medium;
        }

        if (ExpManager.Instance != null)
        {
            ExpManager.Instance.SpawnExp(typeToDrop, dropPos, expReward);
        }
    }
}