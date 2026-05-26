using System.Collections.Generic;
using UnityEngine;

public class TestOrbitDroneWeapon : WeaponBase
{
    [Header("Drone References")]
    public GameObject[] dronePrefabs;
    public GameObject[] droneProjectilePrefabs;
    public ParticleSystem shootParticlePrefab;

    [Header("Target")]
    public LayerMask targetLayer;

    [Header("Orbit Setting")]
    public float orbitHeight = 3f;

    private readonly List<TestOrbitDroneUnit> drones =
        new List<TestOrbitDroneUnit>();

    protected override void OnInitialized()
    {
        SyncDrones(GetCurrentLevelData());
    }

    protected override void OnLevelDataChanged(WeaponLevelData data)
    {
        SyncDrones(data);
    }

    protected override void Attack(WeaponLevelData data)
    {
        SyncDrones(data);
    }

    private void SyncDrones(WeaponLevelData data)
    {
        if (data == null)
            return;

        if (dronePrefabs == null || dronePrefabs.Length == 0)
        {
            Debug.LogWarning("[TestOrbitDroneWeapon] Drone Prefabs 없음");
            return;
        }

        if (droneProjectilePrefabs == null || droneProjectilePrefabs.Length == 0)
        {
            Debug.LogWarning("[TestOrbitDroneWeapon] Drone Projectile Prefabs 없음");
            return;
        }

        int targetCount = Mathf.Max(
            1,
            GetFinalProjectileCount(data.ProjectileCount)
        );

        while (drones.Count < targetCount)
        {
            int prefabIndex = Mathf.Clamp(
                drones.Count,
                0,
                dronePrefabs.Length - 1
            );

            float startAngle = 360f / targetCount * drones.Count;
            float radian = startAngle * Mathf.Deg2Rad;

            float orbitRadius = GetFinalWeaponScale(data.Radius);

            Vector3 offset = new Vector3(
                Mathf.Cos(radian),
                0f,
                Mathf.Sin(radian)
            ) * orbitRadius;

            Vector3 spawnPosition =
                transform.position + offset;

            spawnPosition.y =
                transform.position.y + orbitHeight;

            GameObject droneObject = Instantiate(
                dronePrefabs[prefabIndex],
                spawnPosition,
                dronePrefabs[prefabIndex].transform.rotation
            );

            TestOrbitDroneUnit drone =
                droneObject.GetComponent<TestOrbitDroneUnit>();

            if (drone == null)
                drone = droneObject.AddComponent<TestOrbitDroneUnit>();

            drones.Add(drone);
        }

        while (drones.Count > targetCount)
        {
            TestOrbitDroneUnit removeTarget =
                drones[drones.Count - 1];

            drones.RemoveAt(drones.Count - 1);

            if (removeTarget != null)
                Destroy(removeTarget.gameObject);
        }

        for (int i = 0; i < drones.Count; i++)
        {
            int projectileIndex = Mathf.Clamp(
                i,
                0,
                droneProjectilePrefabs.Length - 1
            );

            float startAngle = 360f / targetCount * i;

            drones[i].Initialize(
                transform,
                droneProjectilePrefabs[projectileIndex],
                shootParticlePrefab,
                targetLayer,
                GetFinalDamage(data.Damage),
                data.Cooldown,
                GetProjectileSpeed(data),
                data.Range,
                data.Radius,
                GetFinalWeaponScale(data.Radius),
                GetDroneRotateSpeed(data),
                orbitHeight,
                startAngle
            );
        }
    }

    private float GetProjectileSpeed(WeaponLevelData data)
    {
        return data.ProjectileSpeed > 0f
            ? data.ProjectileSpeed
            : data.BulletSpeed;
    }

    private float GetDroneRotateSpeed(WeaponLevelData data)
    {
        return data.ProjectileSpeed > 0f
            ? data.ProjectileSpeed * 20f
            : data.BulletSpeed * 20f;
    }
}