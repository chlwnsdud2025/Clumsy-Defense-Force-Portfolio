using UnityEngine;

public class FlyingEnemy : Enemy
{
    [Header("Flying Settings")]
    public float flyingHeightOffset = 1.2f; 

    public override void SpawnSetup(Transform target, Vector3 spawnPosition, EnemyManager manager)
    {
        base.SpawnSetup(target, spawnPosition, manager);
        isTrackingPlayer = true;
    }

    public override void ManagedUpdate(float deltaTime)
    {
        if (playerTarget == null) return;

        // 1. 공중 몹 전용 3D 이동 벡터 계산
        ExecuteBehavior(deltaTime, out Vector3 desiredVelocity);

        // 분리력(Separation)은 Y축을 무시하고 평면적으로만 밀어내게 둡니다. (공중에서 서로 뭉침 방지)
        Vector3 separationForce = myManager.GetSeparationForce(this, separationRadius);
        separationForce.y = 0;

        Vector3 finalVelocity = desiredVelocity + (separationForce * separationWeight);

        if (knockbackVelocity.sqrMagnitude > 0.01f)
        {
            finalVelocity += knockbackVelocity;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, deltaTime * 5f);
        }

        // 2. 중력이나 레이캐스트 없이 곧바로 3D 공간 이동!
        transform.position += finalVelocity * deltaTime;

        // 3. 회전 처리 (플레이어를 내려다보도록 Y축도 살려둠)
        if (isTrackingPlayer)
        {
            Vector3 playerDir = playerTarget.position - transform.position;
            if (playerDir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(playerDir.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * deltaTime);
            }
        }
    }

    protected override void ExecuteBehavior(float deltaTime, out Vector3 desiredVelocity)
    {
        Vector3 targetPos = playerTarget.position + Vector3.up * flyingHeightOffset;
        Vector3 dirToTarget = targetPos - transform.position;
        float distance = dirToTarget.magnitude;
        Vector3 normalizedDir = dirToTarget.normalized;

        desiredVelocity = Vector3.zero;

        if (currentAttackCooldown > 0)
        {
            currentAttackCooldown -= deltaTime;
        }

        if (distance > attackRange)
        {
            // 허공을 가로질러 3D 대각선으로 날아감
            desiredVelocity = normalizedDir * moveSpeed;
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

    protected override void Attack()
    {
        Debug.Log("공중 몹이 플레이어 머리 위에서 공격합니다!");
        // 여기에 투사체를 아래로 쏘거나, 자폭 몹처럼 급강하하는 패턴을 넣을 수 있습니다.
    }
}