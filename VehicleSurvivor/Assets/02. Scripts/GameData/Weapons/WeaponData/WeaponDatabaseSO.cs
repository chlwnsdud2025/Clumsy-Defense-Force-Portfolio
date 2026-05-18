using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponDatabase",
    menuName = "TeamProject/Weapon/Weapon Database"
)]
public class WeaponDatabaseSO : ScriptableObject
{
    [SerializeField] private List<WeaponDefinitionSO> weapons = new();

    private Dictionary<string, WeaponDefinitionSO> _cache;

    private void OnEnable()
    {
        BuildCache();
    }

    private void BuildCache()
    {
        _cache = new Dictionary<string, WeaponDefinitionSO>();

        foreach (WeaponDefinitionSO weapon in weapons)
        {
            if (weapon == null) continue;

            if (string.IsNullOrWhiteSpace(weapon.WeaponId))
            {
                Debug.LogError($"[WeaponDatabaseSO] WeaponId가 비어 있는 무기가 있습니다: {weapon.name}");
                continue;
            }

            if (_cache.ContainsKey(weapon.WeaponId))
            {
                Debug.LogError($"[WeaponDatabaseSO] 중복 WeaponId 발견: {weapon.WeaponId}");
                continue;
            }

            _cache.Add(weapon.WeaponId, weapon);
        }
    }

    public bool TryGetWeapon(string weaponId, out WeaponDefinitionSO weapon)
    {
        if (_cache == null)
            BuildCache();

        return _cache.TryGetValue(weaponId, out weapon);
    }

    public WeaponDefinitionSO GetWeaponOrNull(string weaponId)
    {
        if (TryGetWeapon(weaponId, out WeaponDefinitionSO weapon))
            return weapon;

        Debug.LogError($"[WeaponDatabaseSO] WeaponId를 찾을 수 없습니다: {weaponId}");
        return null;
    }
}