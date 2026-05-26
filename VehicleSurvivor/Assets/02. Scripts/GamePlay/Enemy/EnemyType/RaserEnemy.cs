using UnityEngine;

public class LaserEnemy : Enemy
{
    [Header("Laser Settings")]
    public float telegraphDuration = 2.0f;
    public float laserLength = 20f;
    public float startRadius = 3.0f;       // 예고 원통의 초기 굵기
    public float endRadius = 0.3f;         // 예고 원통이 수축될 최종 굵기
    public float laserDamage = 25f;
    public float heightOffset = 1.0f;
    public LayerMask playerLayer;

    [Header("3D Telegraph Cylinder")]
    public Transform warningCylinderParent; // 방향과 위치를 잡을 부모 빈 오브젝트
    public Transform warningCylinderMesh;   // 실제 수축할 원기둥 메쉬 (X축 90도 회전됨)

    // --- [새로 추가됨] 레이저 가운데에 고정될 진한 핵심 선 ---
    [Header("Laser Core Settings")]
    public Transform laserCoreMesh;        // 수축하지 않는 얇고 진한 원기둥 할당
    public float coreRadius = 0.3f;        // 핵심 선의 굵기 (endRadius와 비슷하게 설정)
    // ----------------------------------------------------

    private bool isCharging = false;
    private float chargeTimer;
    private Vector3 lockedDirection;
    private float originalSeparation;

    public override void SpawnSetup(Transform target, Vector3 spawnPosition, EnemyManager manager)
    {
        base.SpawnSetup(target, spawnPosition, manager);
        isCharging = false;
        originalSeparation = separationWeight;
        isTrackingPlayer = true;

        if (warningCylinderParent != null)
        {
            warningCylinderParent.gameObject.SetActive(false);
        }
    }

    protected override void Attack()
    {
        if (isCharging) return;

        isCharging = true;
        chargeTimer = telegraphDuration;

        // 조준 중 제자리 고정
        originalSeparation = separationWeight;
        separationWeight = 0f;
        isTrackingPlayer = false;

        // 1. 발사할 방향 계산 및 고정
        Vector3 dir = playerTarget.position - transform.position;
        dir.y = 0;
        lockedDirection = dir.normalized;

        if (warningCylinderParent != null)
        {
            warningCylinderParent.gameObject.SetActive(true);

            // 2. 부모 회전/위치 설정 (총구에서 나가는 것처럼 보정)
            warningCylinderParent.rotation = Quaternion.LookRotation(lockedDirection);
            warningCylinderParent.position = transform.position + Vector3.up * heightOffset + (lockedDirection * (laserLength / 2f));

            // 3. 예고 원통은 초기 굵기로 설정
            SetCylinderScale(warningCylinderMesh, startRadius);

            // 4. [새로 추가됨] 핵심 레이저 선은 정해진 얇은 굵기(coreRadius)로 고정
            SetCylinderScale(laserCoreMesh, coreRadius);
        }
    }

    protected override void ExecuteBehavior(float deltaTime, out Vector3 desiredVelocity)
    {
        if (!isCharging)
        {
            base.ExecuteBehavior(deltaTime, out desiredVelocity);
        }
        else
        {
            desiredVelocity = Vector3.zero; // 조준 중 정지
            chargeTimer -= deltaTime;

            // 시간에 따라 예고 원통(warningCylinderMesh)만 수축하는 연출
            if (warningCylinderParent != null && warningCylinderMesh != null)
            {
                float progress = 1.0f - (chargeTimer / telegraphDuration);

                // 반지름이 큰 수치(startRadius)에서 핵심 선 크기(endRadius)까지 수축
                float currentRadius = Mathf.Lerp(startRadius, endRadius, progress);
                SetCylinderScale(warningCylinderMesh, currentRadius);
            }

            // 차징 시간이 끝나면 발사
            if (chargeTimer <= 0f)
            {
                FireLaser();

                // 상태 초기화
                isCharging = false;
                isTrackingPlayer = true;
                separationWeight = originalSeparation;
                if (warningCylinderParent != null) warningCylinderParent.gameObject.SetActive(false);
            }
        }
    }

    // 부모 스케일 오차 보정 기능을 포함한 통합 스케일 조절 함수
    private void SetCylinderScale(Transform targetMesh, float radius)
    {
        if (targetMesh == null) return;

        // X축 90도 회전된 메쉬 기준으로 X, Z가 굵기, Y가 길이입니다.
        if (targetMesh.parent != null)
        {
            Vector3 parentScale = targetMesh.parent.lossyScale;
            targetMesh.localScale = new Vector3(
                radius / parentScale.x,
                (laserLength / 2f) / parentScale.y,
                radius / parentScale.z
            );
        }
        else
        {
            targetMesh.localScale = new Vector3(radius, laserLength / 2f, radius);
        }
    }

    private void FireLaser()
    {
        Debug.Log("레이저 쾅! 데미지 판정 시작");

        // 레이저 판정 박스의 중심점 (보스 위치에서 방향을 따라 레이저 길이의 절반만큼 앞으로 이동한 위치)
        Vector3 boxCenter = transform.position + Vector3.up * heightOffset + (lockedDirection * (laserLength / 2f));

        // 박스의 절반 크기 (Half Extents). Z축이 레이저 길이의 절반입니다.
        Vector3 halfExtents = new Vector3(endRadius, endRadius, laserLength / 2f);

        // Physics.OverlapBox를 이용해 직육면체 형태의 타격 판정을 만듭니다.
        Collider[] hits = Physics.OverlapBox(boxCenter, halfExtents, Quaternion.LookRotation(lockedDirection), playerLayer);

        foreach (Collider hit in hits)
        {
            // 플레이어(또는 IDamageable)를 찾아서 데미지를 입힙니다.
            IDamageable target = hit.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(DamageInfo.Normal(laserDamage));
            }
        }
    }

    // 개발자용: 에디터에서 레이저 타격 범위를 시각적으로 확인하기 위한 기즈모
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && isCharging)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Vector3 boxCenter = transform.position + Vector3.up * heightOffset + (lockedDirection * (laserLength / 2f));
            Vector3 halfExtents = new Vector3(endRadius, endRadius, laserLength / 2f);

            Gizmos.matrix = Matrix4x4.TRS(boxCenter, Quaternion.LookRotation(lockedDirection), Vector3.one);
            Gizmos.DrawCube(Vector3.zero, halfExtents * 2f);
        }
    }
}