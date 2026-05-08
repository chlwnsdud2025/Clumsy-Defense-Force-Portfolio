using UnityEngine;


public enum EnemyType {
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

    protected bool isTrackingPlayer = true;

    protected float currentAttackCooldown; 
    protected Vector3 knockbackVelocity;

    public virtual void SpawnSetup(Transform target, Vector3 spawnPosition, EnemyManager manager)
    {
        myManager = manager;
        playerTarget = target;
        transform.position = spawnPosition;
        gameObject.SetActive(true);
        currentHealth = maxHealth;
        knockbackVelocity = Vector3.zero;
        currentAttackCooldown = 0f; 
    }

    public void ManagedUpdate(float deltaTime)
    {
        if (playerTarget == null) return;

        ExecuteBehavior(deltaTime, out Vector3 desiredVelocity);

        Vector3 separationForce = myManager.GetSeparationForce(this, separationRadius);
        separationForce.y = 0;

        Vector3 finalVelocity = desiredVelocity + (separationForce * separationWeight);

        if (knockbackVelocity.sqrMagnitude > 0.01f)
        {
            finalVelocity += knockbackVelocity;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, deltaTime * 5f);
        }

        transform.position += finalVelocity * deltaTime;

        

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

        // 1. 매 프레임 쿨타임 감소
        if (currentAttackCooldown > 0)
        {
            currentAttackCooldown -= deltaTime;
        }

        // 2. 사거리 밖이면 다가감
        if (distanceToPlayer > attackRange)
        {
            desiredVelocity = playerDir * moveSpeed;
        }
        else
        {
            // 3. 사거리 이내이고 쿨타임이 돌았다면 공격!
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
        dropPos.y = 0.5f;



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