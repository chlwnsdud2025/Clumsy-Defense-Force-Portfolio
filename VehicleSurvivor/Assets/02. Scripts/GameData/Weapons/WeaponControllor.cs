using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("무기 장착 위치")]
    public Transform weaponHolder;

    [Header("시작 무기")]
    public List<WeaponBase> startWeaponPrefabs =
        new List<WeaponBase>();

    private readonly List<WeaponBase>
        currentWeapons =
        new List<WeaponBase>();

    private void Start()
    {
        foreach (
            WeaponBase weaponPrefab
            in startWeaponPrefabs
        )
        {
            AddOrUpgradeWeapon(
                weaponPrefab
            );
        }
    }

    public void AddOrUpgradeWeapon(
        WeaponBase weaponPrefab
    )
    {
        if (
            weaponPrefab
            == null
        )
        {
            Debug.LogWarning(
                "무기 프리팹이 비어있습니다."
            );

            return;
        }

        WeaponBase existingWeapon =
            FindWeapon(
                weaponPrefab
                .GetWeaponID()
            );

        if (
            existingWeapon
            != null
        )
        {
            existingWeapon
                .LevelUpWeapon();
        }
        else
        {
            AddWeapon(
                weaponPrefab
            );
        }
    }

    private void AddWeapon(
        WeaponBase weaponPrefab
    )
    {
        Transform parent =
            weaponHolder != null
            ? weaponHolder
            : transform;

        WeaponBase newWeapon =
            Instantiate(
                weaponPrefab,
                parent.position,
                parent.rotation,
                parent
            );

        currentWeapons.Add(
            newWeapon
        );

        Debug.Log(
            $"{newWeapon.GetWeaponName()} 추가 완료"
        );
    }

    private WeaponBase FindWeapon(
        string weaponID
    )
    {
        foreach (
            WeaponBase weapon
            in currentWeapons
        )
        {
            if (
                weapon
                .GetWeaponID()
                == weaponID
            )
            {
                return weapon;
            }
        }

        return null;
    }
}