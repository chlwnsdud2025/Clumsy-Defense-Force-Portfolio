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
            Debug.LogError("[WeaponInventory] 추가하려는 WeaponDefinitionSO가 null입니다.");
            return false;
        }

        if (HasWeapon(definition.WeaponId))
        {
            Debug.LogWarning($"[WeaponInventory] 이미 보유 중인 무기입니다: {definition.WeaponId}");
            return false;
        }

        WeaponRuntimeData runtimeData = new WeaponRuntimeData(definition);
        weapons.Add(runtimeData);

        SpawnWeapon(runtimeData, owner);

        return true;
    }

    private void SpawnWeapon(WeaponRuntimeData runtimeData, Transform owner)//무기는 보통 플레이어 위치를 기준으로 동작
    {
        GameObject prefab = runtimeData.Definition.WeaponPrefab;

        if (prefab == null)
        {
            Debug.LogError($"[WeaponInventory] {runtimeData.Definition.WeaponName}의 WeaponPrefab이 비어 있습니다.");
            return;
        }

        Transform parent = weaponRoot != null ? weaponRoot : transform;

        GameObject weaponObject = Instantiate(prefab, parent);

      //  if (weaponObject.TryGetComponent(out WeaponBase weaponBase))
      //  {
      //      weaponBase.Initialize(owner, runtimeData);
      //  }
      //  else
        {
            Debug.LogError($"[WeaponInventory] {prefab.name}에 WeaponBase가 없습니다.");
        }
    }

    public bool TryLevelUpWeapon(string weaponId)
    {
        WeaponRuntimeData weapon = weapons.Find(weapon => weapon.Definition.WeaponId == weaponId);

        if (weapon == null)
        {
            Debug.LogWarning($"[WeaponInventory] 레벨업할 무기를 찾지 못했습니다: {weaponId}");
            return false;
        }

        return weapon.TryLevelUp();
    }
}