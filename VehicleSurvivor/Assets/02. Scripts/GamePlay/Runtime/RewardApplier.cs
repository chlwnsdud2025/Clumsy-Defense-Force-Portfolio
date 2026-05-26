using System.Collections.Generic;
using UnityEngine;

public sealed class RewardApplier
{
    private readonly WeaponEvolutionService weaponEvolutionService = new();

    public bool Apply(
        PlayerRuntime player,
        RewardOption reward,
        WeaponDatabaseSO weaponDatabase,
        PlayerWeaponSpawner playerWeaponSpawner,
        Transform owner)
    {
        if (player == null || reward == null)
            return false;

        switch (reward.Type)
        {
            case ERewardType.Weapon:
                return ApplyWeapon(player, reward.Weapon, weaponDatabase, playerWeaponSpawner, owner);
            case ERewardType.Passive:
                return ApplyPassive(player, reward.Passive, weaponDatabase, playerWeaponSpawner, owner);
            case ERewardType.Item:
                return ApplyItem(player, reward.Item);
            case ERewardType.Heal:
                player.Health.Heal(reward.HealAmount);
                return true;
            default:
                return false;
        }
    }

    public bool ApplyStartingWeapon(
        PlayerRuntime player,
        WeaponDefinitionSO definition,
        PlayerWeaponSpawner playerWeaponSpawner,
        Transform owner) =>
        ApplyWeapon(player, definition, null, playerWeaponSpawner, owner);

    private bool ApplyWeapon(
        PlayerRuntime player,
        WeaponDefinitionSO definition,
        WeaponDatabaseSO weaponDatabase,
        PlayerWeaponSpawner playerWeaponSpawner,
        Transform owner)
    {
        if (definition == null)
            return false;

        bool alreadySpawned = playerWeaponSpawner != null && playerWeaponSpawner.HasSpawnedWeapon(definition.WeaponId);
        WeaponInstance weapon = player.Inventory.AddOrLevelUpWeapon(definition);
        if (weapon == null)
            return false;

        if (!alreadySpawned && playerWeaponSpawner != null)
            playerWeaponSpawner.TrySpawnWeapon(weapon, owner);

        weaponEvolutionService.TryEvolveAllEligibleWeapons(player, weaponDatabase, playerWeaponSpawner, owner);
        return true;
    }

    private bool ApplyPassive(
        PlayerRuntime player,
        PassiveDefinitionSO definition,
        WeaponDatabaseSO weaponDatabase,
        PlayerWeaponSpawner playerWeaponSpawner,
        Transform owner)
    {
        if (definition == null)
            return false;

        player.Inventory.AddOrLevelUpPassive(definition);
        weaponEvolutionService.TryEvolveAllEligibleWeapons(player, weaponDatabase, playerWeaponSpawner, owner);
        return true;
    }
    private static bool ApplyItem(PlayerRuntime player, ItemDefinitionSO definition)
    {
        if(definition == null)
        {
            return false;
        }
        player.Inventory.OnlyAddItem(definition);
        return true;
    }
}

public sealed class WeaponEvolutionService
{
    public bool CanEvolveWeapon(PlayerRuntime player, WeaponInstance weapon)
    {
        if (player == null || weapon?.Definition == null)
            return false;

        WeaponDefinitionSO definition = weapon.Definition;
        if (string.IsNullOrWhiteSpace(definition.EvolutionWeaponId))
            return false;

        if (string.IsNullOrWhiteSpace(definition.RequiredPassiveId))
            return false;

        if (!weapon.IsMaxLevel)
            return false;

        return player.Inventory.HasPassive(definition.RequiredPassiveId);
    }

    public bool TryEvolveWeapon(
        PlayerRuntime player,
        WeaponInstance weapon,
        WeaponDatabaseSO weaponDatabase,
        PlayerWeaponSpawner spawner,
        Transform owner)
    {
        if (!CanEvolveWeapon(player, weapon))
            return false;

        if (weaponDatabase == null)
            return false;

        string oldWeaponId = weapon.Definition.WeaponId;
        string evolutionWeaponId = weapon.Definition.EvolutionWeaponId;
        WeaponDefinitionSO evolvedDefinition = weaponDatabase.GetWeaponOrNull(evolutionWeaponId);

        if (evolvedDefinition == null)
            return false;

        WeaponInstance evolvedWeapon = player.Inventory.ReplaceWeapon(weapon, evolvedDefinition);
        if (evolvedWeapon == null)
            return false;

        if (spawner == null)
            return true;

        if (!spawner.ReplaceSpawnedWeapon(oldWeaponId, evolvedWeapon, owner))
        {
            Debug.LogWarning($"[WeaponEvolutionService] Failed to replace spawned weapon: {oldWeaponId} -> {evolutionWeaponId}");
            return false;
        }

        return true;
    }

    public bool TryEvolveAllEligibleWeapons(
        PlayerRuntime player,
        WeaponDatabaseSO weaponDatabase,
        PlayerWeaponSpawner spawner,
        Transform owner)
    {
        if (player == null)
            return false;

        List<WeaponInstance> weapons = new(player.Inventory.Weapons);
        bool evolvedAny = false;

        foreach (WeaponInstance weapon in weapons)
        {
            if (TryEvolveWeapon(player, weapon, weaponDatabase, spawner, owner))
                evolvedAny = true;
        }

        return evolvedAny;
    }
}
