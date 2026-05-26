using UnityEngine;
using static UnityEngine.ParticleSystem;

public class MeteorWeapon : WeaponBase
{

    //Damage = 폭발 데미지
    //Cooldown = 공격 주기
    //Radius = 랜덤 낙하 반경
    //Range = 플레이어 최소 안전 거리
    //ExplosionRadius = 폭발 범위
    //MeteorDelay = 떨어지기 전 대기시간
    //ProjectileSpeed = 낙하 속도

    
    //Cast Particle Prefab = 낙하지점 경고 이펙트
   

    [Header("Meteor References")]
    public GameObject meteorPrefab;
    public ParticleSystem castParticlePrefab;

    [Header("Spawn Option")]
    public float spawnHeight = 15f;

    protected override void Attack(
        WeaponLevelData data
    )
    {
        if (data == null)
            return;

        SpawnMeteor(
            GetFinalDamage(data.Damage),
            GetMeteorSpeed(data),
            GetFinalWeaponScale(data.ExplosionRadius),
            data.MeteorDelay,
            GetFinalWeaponScale(data.Radius),
            data.Range
        );
    }

    private void SpawnMeteor(
        float finalDamage,
        float meteorSpeed,
        float explosionRadius,
        float meteorDelay,
        float randomRadius,
        float safeRange
    )
    {
        if (meteorPrefab == null)
        {
            Debug.LogWarning(
                "[MeteorWeapon] Meteor Prefab 없음"
            );
            return;
        }

        Vector2 randomCircle =
            Random.insideUnitCircle.normalized *
            Random.Range(
                safeRange,
                randomRadius
            );

        Vector3 targetPosition =
            transform.position +
            new Vector3(
                randomCircle.x,
                0f,
                randomCircle.y
            );

        Vector2 randomDirection =
    Random.insideUnitCircle.normalized;

        float horizontalOffset =
            Random.Range(5f, 10f);

        Vector3 spawnPosition =
            targetPosition +
            Vector3.up * spawnHeight +
            new Vector3(
                randomDirection.x * horizontalOffset,
                0f,
                randomDirection.y * horizontalOffset
            );

        if (castParticlePrefab != null)
        {
            Instantiate(
                castParticlePrefab,
                targetPosition,
                Quaternion.identity
            );
        }

        GameObject meteorObject =
            Instantiate(
                meteorPrefab,
                spawnPosition,
                meteorPrefab.transform.rotation
            );

        MeteorProjectile meteor =
            meteorObject.GetComponent<MeteorProjectile>();

        if (meteor == null)
        {
            Debug.LogWarning(
                "[MeteorWeapon] MeteorProjectile 없음"
            );
            return;
        }

        meteor.Initialize(
            finalDamage,
            meteorSpeed,
            explosionRadius,
            meteorDelay,
            targetPosition
        );
    }

    private float GetMeteorSpeed(
        WeaponLevelData data
    )
    {
        return data.ProjectileSpeed > 0f
            ? data.ProjectileSpeed
            : data.BulletSpeed;
    }
}