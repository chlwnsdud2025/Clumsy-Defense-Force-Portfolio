using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponSpawner : MonoBehaviour
{
    [SerializeField] private Transform weaponRoot;
    [SerializeField] private Transform firePoint;

    private readonly Dictionary<string, WeaponBase> spawnedWeapons = new();
    private PlayerRuntime runtime;

    public void BindRuntime(PlayerRuntime runtime)
    {
        this.runtime = runtime;
    }

    public bool HasSpawnedWeapon(string weaponId)
    {
        return !string.IsNullOrWhiteSpace(weaponId) && spawnedWeapons.ContainsKey(weaponId);
    }

    public bool TrySpawnWeapon(WeaponInstance weapon, Transform owner)
    {
        if (weapon == null || weapon.Definition == null)
        {
            Debug.LogError("[PlayerWeaponSpawner] WeaponInstance is null.");
            return false;
        }

        string weaponId = weapon.Definition.WeaponId;
        if (HasSpawnedWeapon(weaponId))
        {
            Debug.LogWarning($"[PlayerWeaponSpawner] Weapon already spawned: {weaponId}");
            return false;
        }

        GameObject prefab = weapon.Definition.WeaponPrefab;
        if (prefab == null)
        {
            Debug.LogError($"[PlayerWeaponSpawner] WeaponPrefab is missing: {weapon.Definition.WeaponName}");
            return false;
        }

        Transform parent = weaponRoot != null ? weaponRoot : transform;
        GameObject weaponObject = Instantiate(prefab, parent);

        if (!weaponObject.TryGetComponent(out WeaponBase weaponBase))
        {
            Debug.LogError($"[PlayerWeaponSpawner] WeaponBase is missing on prefab: {prefab.name}");
            Destroy(weaponObject);
            return false;
        }

        weaponBase.Initialize(owner, weapon, ResolveFirePoint(owner), runtime);
        spawnedWeapons.Add(weaponId, weaponBase);
        return true;
    }

    private Transform ResolveFirePoint(Transform owner)
    {
        if (firePoint != null)
            return firePoint;

        if (owner == null)
            return null;

        Transform[] children = owner.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == "ShootPoint")
            {
                firePoint = child;
                return firePoint;
            }
        }

        return null;
    }
}
