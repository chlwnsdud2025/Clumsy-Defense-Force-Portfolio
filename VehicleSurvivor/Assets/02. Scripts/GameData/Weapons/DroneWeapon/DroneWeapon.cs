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

    [Header("Snake Formation")]
    [Tooltip("드론이 바닥에서 떠 있는 높이")]
    public float hoverHeight = 0.5f;
    [Tooltip("드론 간의 간격 (미터)")]
    public float spacing = 1.5f;
    [Tooltip("경로를 기록하는 해상도 (낮을수록 정밀하게 길을 따라감)")]
    public float recordInterval = 0.1f;
    [Tooltip("최대 기록할 경로의 수 (너무 길어지면 옛날 기록 삭제)")]
    public int maxHistory = 500;

    private readonly List<DroneUnit> drones = new List<DroneUnit>();

    // 플레이어가 지나간 길을 3D 좌표 그대로 기록하는 리스트
    private readonly List<Vector3> pathHistory = new List<Vector3>();

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

    protected override void Update()
    {
        base.Update(); // 쿨타임 계산을 위해 반드시 호출
        RecordPath();
        UpdateDroneTargets();
    }

    private void RecordPath()
    {
        if (pathHistory.Count == 0)
        {
            pathHistory.Add(transform.position);
            return;
        }

        // Vector3.Distance는 X, Y, Z축 모두를 계산하므로 점프나 낙하 시에도 정상적으로 기록됩니다.
        if (Vector3.Distance(transform.position, pathHistory[0]) >= recordInterval)
        {
            pathHistory.Insert(0, transform.position);

            if (pathHistory.Count > maxHistory)
            {
                pathHistory.RemoveAt(pathHistory.Count - 1);
            }
        }
    }

    private void UpdateDroneTargets()
    {
        for (int i = 0; i < drones.Count; i++)
        {
            int targetIndex = Mathf.RoundToInt((i + 1) * spacing / recordInterval);

            Vector3 targetPos = transform.position;

            if (pathHistory.Count > targetIndex)
            {
                targetPos = pathHistory[targetIndex]; // 과거의 3D 좌표(X, Y, Z)를 통째로 가져옴
            }
            else if (pathHistory.Count > 0)
            {
                targetPos = pathHistory[pathHistory.Count - 1];
            }

            // [수정된 부분] 플레이어의 현재 Y값이 아니라, 과거에 기록된 Y값에 hoverHeight만 더해줍니다.
            targetPos.y += hoverHeight;

            drones[i].SetFollowTarget(targetPos);
        }
    }

    private void SyncDrones(WeaponLevelData data)
    {
        if (data == null) return;
        if (dronePrefabs == null || dronePrefabs.Length == 0) return;

        int targetCount = Mathf.Max(1, GetFinalProjectileCount(data.ProjectileCount));

        // 1. 드론 생성
        while (drones.Count < targetCount)
        {
            int prefabIndex = Mathf.Clamp(drones.Count, 0, dronePrefabs.Length - 1);
            GameObject selectedPrefab = dronePrefabs[prefabIndex];

            GameObject droneObject = Instantiate(
                selectedPrefab,
                transform.position,
                selectedPrefab.transform.rotation
            );

            DroneUnit drone = droneObject.GetComponent<DroneUnit>();
            if (drone == null) drone = droneObject.AddComponent<DroneUnit>();

            drones.Add(drone);
        }

        // 2. 드론 삭제
        while (drones.Count > targetCount)
        {
            DroneUnit removeTarget = drones[drones.Count - 1];
            drones.RemoveAt(drones.Count - 1);
            if (removeTarget != null) Destroy(removeTarget.gameObject);
        }

        // 3. 드론 데이터 갱신
        for (int i = 0; i < drones.Count; i++)
        {
            int projectileIndex = Mathf.Clamp(i, 0, droneProjectilePrefabs.Length - 1);
            GameObject projectilePrefab = droneProjectilePrefabs[projectileIndex];

            drones[i].Initialize(
                transform,
                projectilePrefab,
                shootParticlePrefab,
                targetLayer,
                GetFinalDamage(data.Damage),
                data.Cooldown,
                GetProjectileSpeed(data),
                data.Range,
                data.Radius
            );
        }
    }

    private float GetProjectileSpeed(WeaponLevelData data)
    {
        return data.ProjectileSpeed > 0f ? data.ProjectileSpeed : data.BulletSpeed;
    }
}