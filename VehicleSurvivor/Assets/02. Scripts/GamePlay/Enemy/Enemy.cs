using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Enemy : MonoBehaviour
{
    private Transform playerTarget;
    private EnemyManager myManager;

    [Header("Stat")]
    public float moveSpeed = 5f;
    public float maxHealth = 10;
    private float currentHealth;
    public float rotationSpeed = 2f;

    [Header("Exp Reward")]
    public int expReward = 34;

    [Header("Separation Settings")]
    public float separationRadius = 1.5f; // 회피 거리
    public float separationWeight = 0.5f; // 회피 힘


    [Header("Behavior Settings")]
    public float attackRange = 2.0f; //공격 범위 
    private Vector3 knockbackVelocity;

    public void SpawnSetup(Transform target, Vector3 spawnPosition, EnemyManager manager) //생성
    {
        myManager = manager;
        playerTarget = target;
        transform.position = spawnPosition;
        gameObject.SetActive(true);
        currentHealth = maxHealth;
        knockbackVelocity = Vector3.zero;
    }

    
    public void ManagedUpdate(float deltaTime)// Update 대용 -> 모든 몹이 update하면 부담이 너무 큼
    {
        if (playerTarget == null) return;


        Vector3 playerDir = playerTarget.position - transform.position;// 플레이어 방향
        playerDir.y = 0;
        float distanceToPlayer = playerDir.magnitude; // 현재 거리
        playerDir = playerDir.normalized;

        Vector3 moveVelocity = Vector3.zero;

        
        if (distanceToPlayer > attackRange)//공격사거리 밖일떄
        {
            moveVelocity += playerDir * moveSpeed;
        }

        Vector3 separationForce = myManager.GetSeparationForce(this, separationRadius);// 회피방향
        separationForce.y = 0;
        moveVelocity += separationForce * separationWeight;


        //넉백
        if (knockbackVelocity.sqrMagnitude > 0.01f)
        {
            moveVelocity += knockbackVelocity;
            // 마찰력처럼 서서히 밀리는 힘을 줄임 (값이 클수록 빨리 멈춤)
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, deltaTime * 5f);
        }


        transform.position += moveVelocity * deltaTime;//이동

        if (playerDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(playerDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * deltaTime); // 회전
        }
    }

    public void ApplyKnockback(Vector3 pushDirection, float pushPower) // 넉백
    {
        pushDirection.y = 0;
        knockbackVelocity = pushDirection.normalized * pushPower;
    }

    public void TakeDamage(float damage)//피격
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    
    public void Die()//사망
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
        int remainingExp = expReward;

        int largeCount = remainingExp / 50;
        remainingExp %= 50;

        int mediumCount = remainingExp / 10;
        remainingExp %= 10;

        int smallCount = remainingExp;

        
        Vector3 dropPos = transform.position;
        dropPos.y = 0.5f; 

        SpawnOrbs(ExpType.Large, largeCount, dropPos);
        SpawnOrbs(ExpType.Medium, mediumCount, dropPos);
        SpawnOrbs(ExpType.Small, smallCount, dropPos);
    }

    private void SpawnOrbs(ExpType type, int count, Vector3 pos)
    {
        for (int i = 0; i < count; i++)
        {
            if (ExpManager.Instance != null)
            {
                ExpManager.Instance.SpawnExp(type, pos);
            }
        }
    }
}