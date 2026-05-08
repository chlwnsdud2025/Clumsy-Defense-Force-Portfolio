using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Holder")]
    public Transform weaponHolder;

    [Header("Start Weapons")]
    public List<WeaponBase> startWeaponPrefabs = new List<WeaponBase>();

    private List<WeaponBase> currentWeapons = new List<WeaponBase>();

    private void Start()
    {
        foreach (WeaponBase weaponPrefab in startWeaponPrefabs)
        {
            AddOrUpgradeWeapon(weaponPrefab);
        }
    }

    public void AddOrUpgradeWeapon(WeaponBase weaponPrefab)
    {
        if (weaponPrefab == null)
        {
            Debug.LogWarning("추가하려는 무기 프리팹이 비어있습니다.");
            return;
        }

        WeaponBase existingWeapon = FindWeapon(weaponPrefab.weaponID);

        if (existingWeapon != null)
        {
            existingWeapon.LevelUp();
        }
        else
        {
            AddWeapon(weaponPrefab);
        }
    }

    private void AddWeapon(WeaponBase weaponPrefab)
    {
        Transform parent = weaponHolder != null ? weaponHolder : transform;

        WeaponBase newWeapon = Instantiate(
            weaponPrefab,
            parent.position,
            parent.rotation,
            parent
        );

        currentWeapons.Add(newWeapon);

        Debug.Log($"{newWeapon.weaponName} 무기 추가됨");
    }

    private WeaponBase FindWeapon(string weaponID)
    {
        foreach (WeaponBase weapon in currentWeapons)
        {
            if (weapon.weaponID == weaponID)
            {
                return weapon;
            }
        }

        return null;
    }
}