using UnityEngine;

public class RangedEnemy : Enemy
{
    [Header("Mortar Specifics")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Attack Settings")]
    public float bulletArcHeight = 5.0f;     // 포물선 최대 높이
    public float bulletFlyTime = 1.0f;       // 투사체가 날아가는 체공 시간 (이 시간에 맞춰 장판이 커짐!)
    public float mortarDamage = 15f;         // 폭발 데미지
    public float explosionRadius = 3.0f;     // 폭발(장판) 반경

    [Header("3D Telegraph Settings")]
    public Transform targetIndicator;        // 낙하지점에 깔릴 구체 메쉬(반투명)

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

        // 시작 시 구체는 끄고 크기는 0으로
        if (targetIndicator != null)
        {
            targetIndicator.gameObject.SetActive(false);
            targetIndicator.localScale = Vector3.zero;
        }
    }

    protected override void Attack()
    {
        if (isWaitingForHit) return;

        isWaitingForHit = true;
        // 장판이 커지는 타이머를 '투사체 체공 시간'과 완벽히 일치시킵니다.
        flightTimer = bulletFlyTime;

        // 조준(비행) 중에는 제자리에 고정
        originalSeparation = separationWeight;
        separationWeight = 0f;
        isTrackingPlayer = false;

        // 현재 플레이어의 위치를 타겟으로 고정
        lockedTargetPos = playerTarget.position;

        // 1. 공격 시작과 동시에 투사체를 우선 발사합니다!
        FireMortar();

        // 2. 투사체가 발사됨과 동시에 낙하 지점에 구체 장판을 생성합니다.
        if (targetIndicator != null)
        {
            targetIndicator.gameObject.SetActive(true);
            targetIndicator.position = lockedTargetPos; // 플레이어 발 밑 중앙에 위치
            targetIndicator.localScale = Vector3.zero;
        }

        Debug.Log("포물선 투사체 발사 및 3D 구체 예고 시작!");
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

            // 구체 메쉬가 점점 팽창하는 연출
            if (targetIndicator != null)
            {
                float progress = 1.0f - (flightTimer / bulletFlyTime);
                float currentSize = Mathf.Lerp(0f, explosionRadius * 2f, progress);

                // [수정됨] Y축을 0.1f로 찌그러뜨리지 않고, X,Y,Z 모두 동일하게 키워 완벽한 3D 구체를 만듭니다!
                targetIndicator.localScale = new Vector3(currentSize, currentSize, currentSize);
            }

            // 투사체가 착탄하는 시간(flightTimer <= 0)이 되면 장판을 끄고 기본 상태로 복귀합니다.
            if (flightTimer <= 0f)
            {
                isWaitingForHit = false;
                isTrackingPlayer = true;
                separationWeight = originalSeparation;

                if (targetIndicator != null) targetIndicator.gameObject.SetActive(false);
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
                // 총알에게 체공 시간(bulletFlyTime)을 전달하여 적(Enemy)의 장판 팽창 시간과 완벽히 맞춥니다.
                bullet.InitializeParabolic(firePoint.position, lockedTargetPos, bulletArcHeight, mortarDamage, bulletFlyTime, explosionRadius);
            }
        }
    }
}