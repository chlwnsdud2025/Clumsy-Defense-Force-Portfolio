using System.Collections.Generic;
using UnityEngine;

public class DroneWeapon : WeaponBase
{
    [Header("드론 프리팹 설정")]
    public GameObject[] dronePrefabs;

    [Header("유도탄 설정")]
    public GameObject homingProjectilePrefab;

    [Header("레이어 설정")]
    public LayerMask enemyLayer;

    [Header("드론 위치 설정")]
    public Vector3 droneOffset = new Vector3(0f, 2.5f, -2f);
    public float droneSpacing = 1.5f;

    [Header("탐지 설정")]
    public float detectRadius = 15f;

    private readonly List<DroneUnit> drones = new List<DroneUnit>();

    private void Start()
    {
        UpdateDrones();
    }

    protected override void Attack(WeaponLevelData data)
    {
        UpdateDrones();
    }

    private void UpdateDrones()
    {
        WeaponLevelData data = GetCurrentLevelData();

        if (data == null)
            return;

        if (dronePrefabs == null || dronePrefabs.Length == 0)
            return;

        int targetCount = Mathf.Max(
            1,
            GetFinalProjectileCount(data.projectileCount)
        );

        while (drones.Count < targetCount)
        {
            int prefabIndex = Mathf.Clamp(
                drones.Count,
                0,
                dronePrefabs.Length - 1
            );

            GameObject droneObject = Instantiate(
                dronePrefabs[prefabIndex],
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
            Vector3 offset =
                droneOffset +
                new Vector3(
                    (i - (targetCount - 1) * 0.5f) * droneSpacing,
                    0f,
                    0f
                );

            drones[i].Init(
                transform,
                homingProjectilePrefab,
                enemyLayer,
                GetFinalDamage(data.damage),
                data.cooldown,
                data.bulletSpeed,
                offset,
                detectRadius
            );
        }
    }
}

public class DroneUnit : MonoBehaviour
{
    private Transform owner;
    private GameObject homingProjectilePrefab;
    private LayerMask enemyLayer;

    private float damage;
    private float attackCooldown;
    private float projectileSpeed;
    private float detectRadius;

    private Vector3 offset;
    private float attackTimer;

    public void Init(
        Transform newOwner,
        GameObject newHomingProjectilePrefab,
        LayerMask newEnemyLayer,
        float newDamage,
        float newCooldown,
        float newProjectileSpeed,
        Vector3 newOffset,
        float newDetectRadius
    )
    {
        owner = newOwner;
        homingProjectilePrefab = newHomingProjectilePrefab;
        enemyLayer = newEnemyLayer;

        damage = newDamage;
        attackCooldown = newCooldown;
        projectileSpeed = newProjectileSpeed;
        offset = newOffset;
        detectRadius = newDetectRadius;
    }

    private void Update()
    {
        if (owner == null)
            return;

        FollowPlayer();
        TryAttack();
    }

    private void FollowPlayer()
    {
        Vector3 worldOffset = owner.TransformDirection(offset);

        transform.position = owner.position + worldOffset;
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

        FireHomingProjectile(target, direction.normalized);
    }

    private void FireHomingProjectile(Enemy target, Vector3 direction)
    {
        if (homingProjectilePrefab == null)
            return;

        GameObject projectile = Instantiate(
            homingProjectilePrefab,
            transform.position,
            Quaternion.LookRotation(direction)
        );

        DroneHomingProjectile homing =
            projectile.GetComponent<DroneHomingProjectile>();

        if (homing != null)
        {
            homing.Init(
                target.transform,
                damage,
                projectileSpeed,
                enemyLayer
            );
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

            float distance =
                (enemy.transform.position - transform.position).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }
}