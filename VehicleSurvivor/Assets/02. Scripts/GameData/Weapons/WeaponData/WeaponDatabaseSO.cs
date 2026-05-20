using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponDataBaseSO : ScriptableObject
{
    [Header("Weapon Info")]
    public string weaponID;

    public string weaponName;

    [TextArea]
    public string description;
}

[CreateAssetMenu(
    fileName = "WeaponDatabase",
    menuName = "GameData/Weapon/Weapon Database"
)]
public class WeaponDatabaseSO : ScriptableObject
{
    [SerializeField] private List<WeaponDefinitionSO> weapons = new();

    public IReadOnlyList<WeaponDefinitionSO> Weapons => weapons;

    public WeaponDefinitionSO GetWeaponOrNull(string weaponId)
    {
        if (string.IsNullOrWhiteSpace(weaponId))
        {
            return null;
        }

        string normalizedId = weaponId.Trim();

        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponDefinitionSO weapon = weapons[i];

            if (weapon != null && weapon.WeaponId == normalizedId)
            {
                return weapon;
            }
        }

        Debug.LogWarning($"[WeaponDatabaseSO] Weapon not found: {normalizedId}");
        return null;
    }
}
