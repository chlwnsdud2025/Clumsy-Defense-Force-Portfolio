using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponDatabase",
    menuName = "GameData/Weapon/Weapon Database"
)]
public class WeaponDatabaseSO : ScriptableObject
{
    [Header("Reward Pool")]
    [SerializeField] private List<WeaponDefinitionSO> weapons = new();

    [Header("Evolution Lookup Only")]
    [SerializeField] private List<WeaponDefinitionSO> evolutionWeapons = new();

    public IReadOnlyList<WeaponDefinitionSO> Weapons => weapons;
    public IReadOnlyList<WeaponDefinitionSO> EvolutionWeapons => evolutionWeapons;

    public bool IsEvolutionWeapon(WeaponDefinitionSO weapon)
    {
        if (weapon == null || string.IsNullOrWhiteSpace(weapon.WeaponId))
            return false;

        return IsEvolutionWeaponId(weapon.WeaponId);
    }

    public bool IsEvolutionWeaponId(string weaponId)
    {
        if (string.IsNullOrWhiteSpace(weaponId))
            return false;

        string normalizedId = weaponId.Trim();

        if (FindWeaponOrNull(evolutionWeapons, normalizedId) != null)
            return true;

        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponDefinitionSO weapon = weapons[i];

            if (weapon != null && weapon.EvolutionWeaponId == normalizedId)
                return true;
        }

        return false;
    }

    public WeaponDefinitionSO GetWeaponOrNull(string weaponId)
    {
        if (string.IsNullOrWhiteSpace(weaponId))
        {
            return null;
        }

        string normalizedId = weaponId.Trim();

        WeaponDefinitionSO rewardWeapon = FindWeaponOrNull(weapons, normalizedId);
        if (rewardWeapon != null)
            return rewardWeapon;

        WeaponDefinitionSO evolutionWeapon = FindWeaponOrNull(evolutionWeapons, normalizedId);
        if (evolutionWeapon != null)
            return evolutionWeapon;

        Debug.LogWarning($"[WeaponDatabaseSO] Weapon not found: {normalizedId}");
        return null;
    }

    private static WeaponDefinitionSO FindWeaponOrNull(List<WeaponDefinitionSO> source, string weaponId)
    {
        for (int i = 0; i < source.Count; i++)
        {
            WeaponDefinitionSO weapon = source[i];

            if (weapon != null && weapon.WeaponId == weaponId)
                return weapon;
        }

        return null;
    }
}
