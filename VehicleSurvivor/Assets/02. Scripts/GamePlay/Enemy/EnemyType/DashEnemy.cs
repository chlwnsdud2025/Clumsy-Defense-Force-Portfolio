using UnityEngine;
using UnityEngine.Rendering.Universal; // DecalProjector 사용을 위해 필수

public class DashEnemy : Enemy
{
    public enum DashState { None, Telegraph, Dashing }

    [Header("Dash Settings")]
    public float dashSpeed = 40f;
    public float telegraphDuration = 1.0f;
    public float dashOvershoot = 2.0f;

    [Header("Decal Settings")]
    public DecalProjector DashDecal; 
    public float dashWidth = 2.0f;      // 돌진 공격의 너비
    public float decalDepth = 5.0f;     // 바닥 투사 깊이

    private DashState currentState = DashState.None;
    private float stateTimer;
    private Vector3 dashTargetPosition;
    private float originalSeparation;

    // 몹마다 독립적인 쉐이더(장판) 연출을 위한 매터리얼 복사본 캐싱 변수
    private Material decalMaterialInstance;

    public override void SpawnSetup(Transform target, Vector3 spawnPosition, EnemyManager manager)
    {
        base.SpawnSetup(target, spawnPosition, manager);
        currentState = DashState.None;
        originalSeparation = separationWeight;
        isTrackingPlayer = true;

        // [안전장치] 만약 인스펙터에서 할당을 깜빡했다면, 자식 오브젝트에서 직접 찾습니다.
        if (DashDecal == null)
        {
            DashDecal = GetComponentInChildren<DecalProjector>(true);
        }

        // 시작 시 데칼 비활성화 및 매터리얼 독립화
        if (DashDecal != null)
        {
            // 이 몹이 처음 생성되어 전용 매터리얼이 아직 없다면 1번만 복사 생성
            if (decalMaterialInstance == null && DashDecal.material != null)
            {
                decalMaterialInstance = new Material(DashDecal.material);
                DashDecal.material = decalMaterialInstance; // 복사본으로 교체
            }

            // Decal Projector가 붙어있는 '자식 오브젝트' 자체를 끕니다.
            DashDecal.gameObject.SetActive(false);
        }
    }

    protected override void Attack()
    {
        if (currentState != DashState.None) return;

        currentState = DashState.Telegraph;
        stateTimer = telegraphDuration;

        // 방향 및 거리 확정
        Vector3 dirToPlayer = playerTarget.position - transform.position;
        dirToPlayer.y = 0;
        float dashDistance = dirToPlayer.magnitude + dashOvershoot;
        dashTargetPosition = transform.position + dirToPlayer.normalized * dashDistance;

        isTrackingPlayer = false;

        // 데칼 장판 켜기
        if (DashDecal != null && decalMaterialInstance != null)
        {
            DashDecal.gameObject.SetActive(true);

            // 위치: 자식 오브젝트의 월드 좌표를 현재 위치와 목표 지점의 중간으로 이동
            Vector3 centerPos = transform.position + dirToPlayer.normalized * (dashDistance / 2f);
            DashDecal.transform.position = centerPos;

            // 회전: 바닥을 향하게(down) 하되, Y축을 돌진 방향으로 정렬
            DashDecal.transform.rotation = Quaternion.LookRotation(Vector3.down, dirToPlayer.normalized);

            // 크기 설정: X(너비), Y(길이), Z(투사 깊이)
            DashDecal.size = new Vector3(dashWidth, dashDistance, decalDepth);

            // 쉐이더 값 초기화 (원본이 아닌 독립된 매터리얼의 값을 0으로 변경)
            decalMaterialInstance.SetFloat("_FillAmount", 0f);
        }

        Debug.Log("돌진 경고! 장판 생성");
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
            desiredVelocity = Vector3.zero;
            separationWeight = 0f;

            stateTimer -= deltaTime;

            // 데칼 차오르는 쉐이더 연출
            if (DashDecal != null && decalMaterialInstance != null)
            {
                // 시간에 따른 진행도 계산 (0.0 ~ 1.0)
                float progress = 1.0f - (stateTimer / telegraphDuration);
                decalMaterialInstance.SetFloat("_FillAmount", progress);
            }

            if (stateTimer <= 0f)
            {
                currentState = DashState.Dashing;

                // 데칼 비활설ㅇ롸
                if (DashDecal != null)
                    DashDecal.gameObject.SetActive(false);

                Debug.Log("돌진 발사!");
            }
        }
        else // DashState.Dashing
        {
            separationWeight = 0f;

            Vector3 toTarget = dashTargetPosition - transform.position;
            toTarget.y = 0;
            float distance = toTarget.magnitude;
            float moveStep = dashSpeed * deltaTime;

            if (distance <= moveStep)
            {
                desiredVelocity = toTarget.normalized * (distance / deltaTime);
                currentState = DashState.None;
                isTrackingPlayer = true;
                Debug.Log("돌진 완료!");
            }
            else
            {
                desiredVelocity = toTarget.normalized * dashSpeed;
            }
        }
    }

    // 오브젝트(또는 몹 프리팹) 파괴 시 생성했던 매터리얼 복사본을 메모리에서 해제합니다.
    private void OnDestroy()
    {
        if (decalMaterialInstance != null)
        {
            Destroy(decalMaterialInstance);
        }
    }
}