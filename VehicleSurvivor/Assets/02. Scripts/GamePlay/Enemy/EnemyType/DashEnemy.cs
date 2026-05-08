using UnityEngine;

public class DashEnemy : Enemy
{
    public enum DashState { None, Telegraph, Dashing }

    [Header("Dash Settings")]
    public float dashSpeed = 40f;
    public float telegraphDuration = 1.0f;
    public float dashOvershoot = 2.0f;

    private DashState currentState = DashState.None;
    private float stateTimer;
    private Vector3 dashTargetPosition;
    private float originalSeparation;

    public override void SpawnSetup(Transform target, Vector3 spawnPosition, EnemyManager manager)
    {
        base.SpawnSetup(target, spawnPosition, manager);
        currentState = DashState.None;
        originalSeparation = separationWeight;
        isTrackingPlayer = true; 
    }

    protected override void Attack()
    {
        if (currentState != DashState.None) return;

        // 1. 공격 발동! 즉시 텔레그래프(경고) 상태로 진입
        currentState = DashState.Telegraph;
        stateTimer = telegraphDuration;

        // 경고가 시작되는 '이 순간'의 플레이어 위치를 기준으로 모든 것을 확정!
        Vector3 dirToPlayer = playerTarget.position - transform.position;
        dirToPlayer.y = 0;

        dashTargetPosition = transform.position + dirToPlayer.normalized * (dirToPlayer.magnitude + dashOvershoot);

       
        isTrackingPlayer = false;

        Debug.Log("돌진 경고! (방향 및 거리 확정)");
    }

    protected override void ExecuteBehavior(float deltaTime, out Vector3 desiredVelocity)
    {
        if (currentState == DashState.None)
        {
            base.ExecuteBehavior(deltaTime, out desiredVelocity);
            separationWeight = originalSeparation;
        }
        else if (currentState == DashState.Telegraph)
        {
            // 2. 경고 상태: 제자리에 멈춰서 기를 모음
            desiredVelocity = Vector3.zero;
            separationWeight = 0f;

            stateTimer -= deltaTime;
            if (stateTimer <= 0f)
            {
                currentState = DashState.Dashing;
                Debug.Log("돌진 발사!");
            }
        }
        else // DashState.Dashing
        {
            // 3. 돌진 상태
            separationWeight = 0f;

            Vector3 toTarget = dashTargetPosition - transform.position;
            toTarget.y = 0;
            float distance = toTarget.magnitude;
            float moveStep = dashSpeed * deltaTime;

            // 목표 지점에 도달했는지 체크
            if (distance <= moveStep)
            {
                desiredVelocity = toTarget.normalized * (distance / deltaTime);

                // 돌진이 끝났으므로 상태 초기화 및 고개 회전 잠금 해제!
                currentState = DashState.None;
                isTrackingPlayer = true;

                Debug.Log("돌진 완료! 다시 추적 시작!");
            }
            else
            {
                // 확정된 목표 지점을 향해 전속력 이동
                desiredVelocity = toTarget.normalized * dashSpeed;
            }
        }
    }
}