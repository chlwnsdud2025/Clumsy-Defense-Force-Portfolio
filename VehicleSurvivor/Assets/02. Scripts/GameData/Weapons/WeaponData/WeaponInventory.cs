using System.Collections.Generic;
using UnityEngine;

public class WeaponInventory : MonoBehaviour
{
    [SerializeField] private Transform weaponRoot;

    private readonly List<WeaponRuntimeData> weapons = new();

    public IReadOnlyList<WeaponRuntimeData> Weapons => weapons;

    public bool HasWeapon(string weaponId)
    {
        return weapons.Exists(weapon => weapon.Definition.WeaponId == weaponId);
    }

    public bool TryAddWeapon(WeaponDefinitionSO definition, Transform owner)
    {
        if (definition == null)
        {
            Debug.LogError("[WeaponInventory] WeaponDefinitionSO is null.");
            return false;
        }

        if (HasWeapon(definition.WeaponId))
        {
            Debug.LogWarning($"[WeaponInventory] Weapon already owned: {definition.WeaponId}");
            return false;
        }

        WeaponRuntimeData runtimeData = new WeaponRuntimeData(definition);
        weapons.Add(runtimeData);

        SpawnWeapon(runtimeData, owner);

        return true;
    }

    private void SpawnWeapon(WeaponRuntimeData runtimeData, Transform owner)
    {
        GameObject prefab = runtimeData.Definition.WeaponPrefab;

        if (prefab == null)
        {
            Debug.LogError($"[WeaponInventory] WeaponPrefab is missing: {runtimeData.Definition.WeaponName}");
            return;
        }

        Transform parent = weaponRoot != null ? weaponRoot : transform;
        GameObject weaponObject = Instantiate(prefab, parent);

        if (weaponObject.TryGetComponent(out WeaponBase weaponBase))
        {
            weaponBase.Initialize(owner, runtimeData);
            return;
        }

        Debug.LogError($"[WeaponInventory] WeaponBase is missing on prefab: {prefab.name}");
    }

    public bool TryLevelUpWeapon(string weaponId)
    {
        WeaponRuntimeData weapon = weapons.Find(weapon => weapon.Definition.WeaponId == weaponId);

        if (weapon == null)
        {
            Debug.LogWarning($"[WeaponInventory] Weapon not found for level up: {weaponId}");
            return false;
        }

        return weapon.TryLevelUp();
    }
}
