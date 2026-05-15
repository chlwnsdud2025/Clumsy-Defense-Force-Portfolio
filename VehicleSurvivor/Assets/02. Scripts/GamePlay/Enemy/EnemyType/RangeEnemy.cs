using UnityEngine;

public class RangedEnemy : Enemy
{
    [Header("Mortar Specifics")]
    [Tooltip("이 몹이 발사할 투사체 프리팹 (투사체마다 다른 메쉬 사용 가능)")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Attack Settings")]
    public float bulletArcHeight = 5.0f;
    public float bulletFlyTime = 1.0f;
    public float mortarDamage = 15f;
    public float explosionRadius = 3.0f;

    [Header("VFX")]
    [Tooltip("이 몹의 투사체가 터질 때 사용할 폭발 이펙트")]
    public GameObject explosionVfxPrefab;

    [Header("3D Telegraph Settings")]
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

        // 1. 투사체 발사 (이펙트 정보 전달 포함)
        FireMortar();

        // 2. 장판 생성
        if (indicatorPrefab != null)
        {
            GameObject indicatorObj = Instantiate(indicatorPrefab, lockedTargetPos, Quaternion.identity);
            TargetIndicator indicatorScript = indicatorObj.GetComponent<TargetIndicator>();

            if (indicatorScript != null)
            {
                indicatorScript.Initialize(explosionRadius, bulletFlyTime);
            }
        }
    }

    protected override void ExecuteBehavior(float deltaTime, out Vector3 desiredVelocity)
    {
        if (!isWaitingForHit)
        {
            base.ExecuteBehavior(deltaTime, out desiredVelocity);
        }
        else
        {
            desiredVelocity = Vector3.zero;
            flightTimer -= deltaTime;

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
            // 이 몹 전용 투사체 프리팹 생성
            GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            EnemyBullet bullet = bulletObj.GetComponent<EnemyBullet>();

            if (bullet != null)
            {
                // [핵심] 투사체에게 이펙트(explosionVfxPrefab) 정보까지 함께 넘겨줍니다.
                bullet.InitializeParabolic(
                    firePoint.position,
                    lockedTargetPos,
                    bulletArcHeight,
                    mortarDamage,
                    bulletFlyTime,
                    explosionRadius,
                    explosionVfxPrefab
                );
            }
        }
    }
}