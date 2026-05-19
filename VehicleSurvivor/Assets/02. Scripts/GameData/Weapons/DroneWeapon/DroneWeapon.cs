using System.Collections.Generic;
using UnityEngine;

public class DroneWeapon : WeaponBase
{
    [Header("드론 설정")]
    public GameObject dronePrefab;
    public LayerMask enemyLayer;

    [Header("탐지 범위")]
    public float detectRadius = 15f;

    [Header("이펙트 설정")]
    public GameObject hitEffectPrefab;

    private readonly List<DroneUnit> drones = new List<DroneUnit>();

    private void Start()
    {
        SyncDrones(GetCurrentLevelData());
    }

    protected override void Attack(WeaponLevelData data)
    {
        SyncDrones(data);
    }

    private void SyncDrones(WeaponLevelData data)
    {
        if (data == null || dronePrefab == null)
            return;

        int targetCount = Mathf.Max(1, GetFinalProjectileCount(data.ProjectileCount));

        while (drones.Count < targetCount)
        {
            GameObject droneObject = Instantiate(
                dronePrefab,
                transform.position,
                Quaternion.identity,
                transform
            );

            DroneUnit drone = droneObject.GetComponent<DroneUnit>();

            if (drone == null)
            {
                drone = droneObject.AddComponent<DroneUnit>();
            }

            drones.Add(drone);
        }

        while (drones.Count > targetCount)
        {
            DroneUnit removeTarget = drones[drones.Count - 1];
            drones.RemoveAt(drones.Count - 1);
            Destroy(removeTarget.gameObject);
        }

        for (int i = 0; i < drones.Count; i++)
        {
            float angle = (360f / drones.Count) * i;

            drones[i].Init(
                transform,
                enemyLayer,
                hitEffectPrefab,
                GetFinalDamage(data.Damage),
                GetFinalCooldown(data.Cooldown),
                data.radius,
                data.bulletSpeed,
                angle,
                detectRadius
            );
        }
    }
}

public class DroneUnit : MonoBehaviour
{
    private Transform owner;
    private LayerMask enemyLayer;
    private GameObject hitEffectPrefab;

    private float damage;
    private float attackCooldown;
    private float orbitRadius;
    private float orbitSpeed;
    private float startAngle;
    private float detectRadius;
    private float attackTimer;

    public void Init(
        Transform newOwner,
        LayerMask newEnemyLayer,
        GameObject newHitEffectPrefab,
        float newDamage,
        float newAttackCooldown,
        float newOrbitRadius,
        float newOrbitSpeed,
        float newStartAngle,
        float newDetectRadius
    )
    {
        owner = newOwner;
        enemyLayer = newEnemyLayer;
        hitEffectPrefab = newHitEffectPrefab;

        damage = newDamage;
        attackCooldown = newAttackCooldown;
        orbitRadius = newOrbitRadius;
        orbitSpeed = newOrbitSpeed;
        startAngle = newStartAngle;
        detectRadius = newDetectRadius;
    }

    private void Update()
    {
        if (owner == null)
            return;

        OrbitAroundOwner();
        TryAttack();
    }

    private void OrbitAroundOwner()
    {
        float angle = startAngle + Time.time * orbitSpeed;
        float radian = angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(radian),
            0f,
            Mathf.Sin(radian)
        ) * orbitRadius;

        transform.position = owner.position + offset;
    }

    private void TryAttack()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer < attackCooldown)
            return;

        Enemy target = FindClosestEnemy();

        if (target == null)
            return;

        attackTimer = 0f;

        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0f;

        if (direction == Vector3.zero)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized);

        if (Physics.Raycast(
            transform.position,
            direction.normalized,
            out RaycastHit hit,
            detectRadius,
            enemyLayer
        ))
        {
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);

                if (hitEffectPrefab != null)
                {
                    GameObject effect = Instantiate(
                        hitEffectPrefab,
                        hit.point,
                        Quaternion.identity
                    );

                    Destroy(effect, 2f);
                }

                Debug.Log($"드론 공격 성공: {enemy.name}");
            }
        }
    }

    private Enemy FindClosestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectRadius,
            enemyLayer
        );

        Enemy closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
                continue;

            Enemy enemy = hit.GetComponentInParent<Enemy>();

            if (enemy == null)
                continue;

            float distance = (
                enemy.transform.position - transform.position
            ).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
