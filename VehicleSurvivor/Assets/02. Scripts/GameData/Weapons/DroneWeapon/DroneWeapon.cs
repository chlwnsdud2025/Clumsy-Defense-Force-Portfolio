using System.Collections.Generic;
using UnityEngine;

public class DroneWeapon : WeaponBase
{
    [Header("Drone References")]
    public GameObject[] dronePrefabs;
    public GameObject[] droneProjectilePrefabs;
    public ParticleSystem shootParticlePrefab;

    [Header("Target")]
    public LayerMask targetLayer;

    [Header("Drone Position")]
    public Vector3 baseOffset =
        new Vector3(0f, 2.5f, -2f);

    public float spacing = 1.5f;

    private readonly List<DroneUnit> drones =
        new List<DroneUnit>();

    protected override void OnInitialized()
    {
        SyncDrones(GetCurrentLevelData());
    }

    protected override void OnLevelDataChanged(
        WeaponLevelData data
    )
    {
        SyncDrones(data);
    }

    protected override void Attack(
        WeaponLevelData data
    )
    {
        SyncDrones(data);
    }

    private void SyncDrones(
        WeaponLevelData data
    )
    {
        if (data == null)
            return;

        if (dronePrefabs == null ||
            dronePrefabs.Length == 0)
        {
            Debug.LogWarning(
                "[DroneWeapon] Drone Prefabs 없음"
            );
            return;
        }

        int targetCount =
            Mathf.Max(
                1,
                GetFinalProjectileCount(
                    data.ProjectileCount
                )
            );

        while (drones.Count < targetCount)
        {
            int prefabIndex =
                Mathf.Clamp(
                    drones.Count,
                    0,
                    dronePrefabs.Length - 1
                );

            GameObject selectedPrefab =
                dronePrefabs[prefabIndex];

            GameObject droneObject =
                Instantiate(
                    selectedPrefab,
                    transform.position,
                    selectedPrefab.transform.rotation,
                    transform
                );

            DroneUnit drone =
                droneObject.GetComponent<DroneUnit>();

            if (drone == null)
            {
                drone =
                    droneObject.AddComponent<DroneUnit>();
            }

            drones.Add(drone);
        }

        while (drones.Count > targetCount)
        {
            DroneUnit removeTarget =
                drones[drones.Count - 1];

            drones.RemoveAt(
                drones.Count - 1
            );

            if (removeTarget != null)
            {
                Destroy(
                    removeTarget.gameObject
                );
            }
        }

        for (int i = 0; i < drones.Count; i++)
        {
            Vector3 offset =
                baseOffset +
                new Vector3(
                    (
                        i -
                        (targetCount - 1) * 0.5f
                    ) * spacing,
                    0f,
                    0f
                );

            int projectileIndex =
                Mathf.Clamp(
                    i,
                    0,
                    droneProjectilePrefabs.Length - 1
                );

            GameObject projectilePrefab =
                droneProjectilePrefabs[
                    projectileIndex
                ];

            drones[i].Initialize(
                transform,
                projectilePrefab,
                shootParticlePrefab,
                targetLayer,
                GetFinalDamage(
                    data.Damage
                ),
                data.Cooldown,
                GetProjectileSpeed(
                    data
                ),
                data.Range,
                data.Radius,
                offset
            );
        }
    }

    private float GetProjectileSpeed(
        WeaponLevelData data
    )
    {
        return
            data.ProjectileSpeed > 0f
            ? data.ProjectileSpeed
            : data.BulletSpeed;
    }
}