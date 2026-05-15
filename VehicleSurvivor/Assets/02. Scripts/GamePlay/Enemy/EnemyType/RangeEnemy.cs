using UnityEngine;

public class RangedEnemy : Enemy
{
    [Header("Mortar Specifics")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Attack Settings")]
    public float bulletArcHeight = 5.0f;
    public float bulletFlyTime = 1.0f;
    public float mortarDamage = 15f;
    public float explosionRadius = 3.0f;

    [Header("3D Telegraph Settings")]
    // [변경됨] Transform 대신 프리팹을 직접 받습니다.
    public GameObject indicatorPrefab;

    private bool isWaitingForHit = false;
    private float flightTimer;
    private Vector3 lockedTargetPos;
    private float originalSeparation;

    public override void SpawnSetup(Transform target, Vector3 spawnPosition, EnemyManager manager)
    {
        base.SpawnSetup(target, spawnPosition, manager);
        isWaitingForHit = false;
        originalSeparation = separationWeight;
        isTrackingPlayer = true;
    }

    protected override void Attack()
    {
        if (isWaitingForHit) return;

        isWaitingForHit = true;
        flightTimer = bulletFlyTime;

        originalSeparation = separationWeight;
        separationWeight = 0f;
        isTrackingPlayer = false;

        lockedTargetPos = playerTarget.position;

        // 1. 투사체 발사
        FireMortar();

        // 2. [변경됨] 독립된 장판 프리팹 생성 및 초기화
        if (indicatorPrefab != null)
        {
            GameObject indicatorObj = Instantiate(indicatorPrefab, lockedTargetPos, Quaternion.identity);
            TargetIndicator indicatorScript = indicatorObj.GetComponent<TargetIndicator>();

            if (indicatorScript != null)
            {
                // 장판에게 최종 크기와 체공 시간을 넘겨주어 스스로 커지게 만듭니다.
                indicatorScript.Initialize(explosionRadius, bulletFlyTime);
            }
        }

        Debug.Log("포물선 투사체 발사 및 독립형 3D 구체 예고 시작!");
    }

    protected override void ExecuteBehavior(float deltaTime, out Vector3 desiredVelocity)
    {
        if (!isWaitingForHit)
        {
            base.ExecuteBehavior(deltaTime, out desiredVelocity);
        }
        else
        {
            desiredVelocity = Vector3.zero; // 제자리 정지
            flightTimer -= deltaTime;

            // [변경됨] 장판 크기를 조작하던 코드가 삭제되었습니다. (TargetIndicator.cs가 대신 처리함)

            // 투사체 착탄 시간이 지나면 몹의 상태만 기본으로 복귀시킵니다.
            if (flightTimer <= 0f)
            {
                isWaitingForHit = false;
                isTrackingPlayer = true;
                separationWeight = originalSeparation;
            }
        }
    }

    private void FireMortar()
    {
        if (bulletPrefab != null && firePoint != null)
        {
            GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            EnemyBullet bullet = bulletObj.GetComponent<EnemyBullet>();

            if (bullet != null)
            {
                bullet.InitializeParabolic(firePoint.position, lockedTargetPos, bulletArcHeight, mortarDamage, bulletFlyTime, explosionRadius);
            }
        }
    }
}