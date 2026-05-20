using System.Collections.Generic;
using UnityEngine;

public sealed class RewardSystem
{
    private readonly WeaponDatabaseSO weaponDatabase;
    private readonly PassiveTableSO passiveTable;

    public RewardSystem(WeaponDatabaseSO weaponDatabase, PassiveTableSO passiveTable)
    {
        this.weaponDatabase = weaponDatabase;
        this.passiveTable = passiveTable;
    }

    public List<RewardOption> Generate(PlayerRuntime player, int count)
    {
        List<RewardOption> candidates = new();

        if (player != null)
        {
            AddOwnedWeaponUpgradeCandidates(player, candidates);
            AddOwnedPassiveUpgradeCandidates(player, candidates);
            AddNewWeaponCandidates(player, candidates);
            AddNewPassiveCandidates(player, candidates);
        }

        if (candidates.Count == 0 && player != null)
            candidates.Add(RewardOption.HealReward(Mathf.Max(1f, player.Health.MaxHp * 0.25f)));

        return PickRandom(candidates, Mathf.Max(0, count));
    }

    private void AddOwnedWeaponUpgradeCandidates(PlayerRuntime player, List<RewardOption> candidates)
    {
        foreach (WeaponInstance weapon in player.Inventory.Weapons)
        {
            if (weapon != null && !weapon.IsMaxLevel)
                candidates.Add(RewardOption.WeaponReward(weapon.Definition));
        }
    }

    private void AddOwnedPassiveUpgradeCandidates(PlayerRuntime player, List<RewardOption> candidates)
    {
        foreach (PassiveInstance passive in player.Inventory.Passives)
        {
            if (passive != null && !passive.IsMaxLevel)
                candidates.Add(RewardOption.PassiveReward(passive.Definition));
        }
    }

    private void AddNewWeaponCandidates(PlayerRuntime player, List<RewardOption> candidates)
    {
        if (weaponDatabase == null)
            return;

        if (!player.Inventory.CanAddWeapon())
            return;

        foreach (WeaponDefinitionSO weapon in weaponDatabase.Weapons)
        {
            if (weapon != null && !HasWeapon(player, weapon.WeaponId))
                candidates.Add(RewardOption.WeaponReward(weapon));
        }
    }

    private void AddNewPassiveCandidates(PlayerRuntime player, List<RewardOption> candidates)
    {
        if (passiveTable == null)
            return;

        if (!player.Inventory.CanAddPassive())
            return;

        foreach (PassiveDefinitionSO passive in passiveTable.Passives)
        {
            if (passive != null && !HasPassive(player, passive.PassiveId))
                candidates.Add(RewardOption.PassiveReward(passive));
        }
    }

    private static bool HasWeapon(PlayerRuntime player, string weaponId)
    {
        foreach (WeaponInstance weapon in player.Inventory.Weapons)
        {
            if (weapon != null && weapon.Definition.WeaponId == weaponId)
                return true;
        }

        return false;
    }

    private static bool HasPassive(PlayerRuntime player, string passiveId)
    {
        foreach (PassiveInstance passive in player.Inventory.Passives)
        {
            if (passive != null && passive.Definition.PassiveId == passiveId)
                return true;
        }

        return false;
    }

    private static List<RewardOption> PickRandom(List<RewardOption> candidates, int count)
    {
        List<RewardOption> pool = new(candidates);
        List<RewardOption> selected = new();

        while (selected.Count < count && pool.Count > 0)
        {
            int index = Random.Range(0, pool.Count);
            selected.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return selected;
    }
}
