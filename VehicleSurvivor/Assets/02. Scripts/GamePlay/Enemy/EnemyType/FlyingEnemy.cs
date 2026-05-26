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

        // [추가] 공중 몹도 고도가 낮아지거나 플레이어가 점프해서 닿으면 몸통 데미지를 줍니다!
        CheckContactDamage(deltaTime);

        // 1. 공중 몹 전용 3D 이동 벡터 계산
        ExecuteBehavior(deltaTime, out Vector3 desiredVelocity);

        // 분리력(Separation)은 Y축을 무시하고 평면적으로만 밀어내게 둡니다.
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

        // 3. 회전 처리
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

        // [수정] Y축이 포함된 완벽한 3D 거리를 기준으로 특수 패턴 실행 여부를 잽니다.
        float distance3D = Vector3.Distance(transform.position, targetPos);
        Vector3 dirToTarget = (targetPos - transform.position).normalized;

        desiredVelocity = Vector3.zero;

        // [수정] 변경된 변수명 사용
        if (currentPatternCooldown > 0)
        {
            currentPatternCooldown -= deltaTime;
        }

        if (distance3D > patternRange)
        {
            // 허공을 가로질러 3D 대각선으로 날아감
            desiredVelocity = dirToTarget * moveSpeed;
        }
        else
        {
            if (currentPatternCooldown <= 0)
            {
                Attack();
                currentPatternCooldown = patternCooltime;
            }
        }
    }

    protected override void Attack()
    {
        Debug.Log("공중 몹이 플레이어 머리 위에서 사격/폭격 패턴을 실행합니다!");
    }
}