using UnityEngine;

public class SuicideEnemy : Enemy
{
    [Header("Suicide Settings")]
    public float fuseTime = 1.0f;
    public float explosionRadius = 3.0f;
    public float explosionDamage = 50f;

    // --- 기존 데칼 관련 변수를 3D 구체 변수로 교체 ---
    [Header("3D Telegraph Settings")]
    public Transform warningSphere;       // 자식 오브젝트의 반투명 구체 할당
    public float sphereHeightOffset = 1.0f; // 구체의 중심 높이 (몹의 몸통 중앙쯤으로 설정)

    [Header("Hit Settings")]
    public LayerMask playerLayer;         // 플레이어 레이어 꼭 할당해주세요!

    private bool isFusing = false;
    private float fuseTimer;
    private float originalSeparation;

    public override void SpawnSetup(Transform target, Vector3 spawnPosition, EnemyManager manager)
    {
        base.SpawnSetup(target, spawnPosition, manager);
        isFusing = false;
        originalSeparation = separationWeight;
        isTrackingPlayer = true;

        // 스폰 시 구체 초기화
        if (warningSphere != null)
        {
            warningSphere.gameObject.SetActive(false);
            warningSphere.localScale = Vector3.zero;
        }
    }

    protected override void Attack()
    {
        if (isFusing) return;

        isFusing = true;
        fuseTimer = fuseTime;

        originalSeparation = separationWeight;
        separationWeight = 0f;
        isTrackingPlayer = false; // 자폭 시퀀스가 시작되면 플레이어 추적을 멈춤

        // 3D 구체 장판 켜기
        if (warningSphere != null)
        {
            warningSphere.gameObject.SetActive(true);
            warningSphere.position = transform.position + Vector3.up * sphereHeightOffset;
            warningSphere.localScale = Vector3.zero;
        }

        Debug.Log("자폭병 점화! 3D 팽창 시작! 폭발 반경: " + explosionRadius);
    }

    protected override void ExecuteBehavior(float deltaTime, out Vector3 desiredVelocity)
    {
        if (!isFusing)
        {
            base.ExecuteBehavior(deltaTime, out desiredVelocity);
        }
        else
        {
            desiredVelocity = Vector3.zero; // 제자리 정지
            fuseTimer -= deltaTime;

            // 구체가 풍선처럼 점점 부풀어 오르는 3D 연출
            if (warningSphere != null)
            {
                float progress = 1.0f - (fuseTimer / fuseTime);

                // 반지름(Radius)의 2배가 전체 지름(Scale)이 되므로 * 2f 해줍니다.
                float currentDiameter = Mathf.Lerp(0f, explosionRadius * 2f, progress);

                // X, Y, Z 축 모두 동일하게 키워 완벽한 구형을 유지합니다.
                warningSphere.localScale = new Vector3(currentDiameter, currentDiameter, currentDiameter);

                // 몹이 다른 몹에게 밀리거나 넉백을 받을 경우를 대비해 위치를 계속 몹에게 고정시킵니다.
                warningSphere.position = transform.position + Vector3.up * sphereHeightOffset;
            }

            if (fuseTimer <= 0f)
            {
                Explode();
            }
        }
    }

    private void Explode()
    {
        Debug.Log("자폭 쾅!");

        // 1. 시각적 구체 숨기기
        if (warningSphere != null)
        {
            warningSphere.gameObject.SetActive(false);
        }

        // 2. Physics.OverlapSphere를 이용한 실제 데미지 판정
        // 구체의 시각적 크기(explosionRadius)와 완벽히 일치하는 범위 내의 타겟(Player Layer)을 찾습니다.
        Vector3 explosionCenter = transform.position + Vector3.up * sphereHeightOffset;
        Collider[] hitColliders = Physics.OverlapSphere(explosionCenter, explosionRadius, playerLayer);

        foreach (Collider hit in hitColliders)
        {
            IDamageable target = hit.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(explosionDamage);
            }
        }

        // 3. 자폭 시 경험치는 드롭하지 않고 풀(Pool)로 돌아갑니다.
        if (myManager != null)
        {
            myManager.ReturnToPool(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // 개발자 확인용 기즈모 (에디터 씬 뷰에서 빨간 선으로 실제 물리적 폭발 범위를 표시해 줍니다)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 center = transform.position + Vector3.up * sphereHeightOffset;
        Gizmos.DrawWireSphere(center, explosionRadius);
    }
}