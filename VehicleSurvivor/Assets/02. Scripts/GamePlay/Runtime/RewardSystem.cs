using System.Collections.Generic;
using UnityEngine;

public sealed class RewardSystem
{
    private readonly WeaponDatabaseSO weaponDatabase;
    private readonly PassiveTableSO passiveTable;
    private readonly ItemTableSO itemTable;

    public RewardSystem(WeaponDatabaseSO weaponDatabase, PassiveTableSO passiveTable, ItemTableSO itemTable)
    {
        this.weaponDatabase = weaponDatabase;
        this.passiveTable = passiveTable;
        this.itemTable = itemTable;
    }
    


    

    public List<RewardOption> GenerateLevelUp(PlayerRuntime player, int count) //레벨업시 3개 선택지 무기, 패시브 중에 랜덤하나
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
        {
            candidates.Add(RewardOption.HealReward(Mathf.Max(1f, player.Health.MaxHp * 0.25f)));
        }

        return PickRandom(candidates, Mathf.Max(0, count));
    }

    public List<RewardOption> GenerateChest(PlayerRuntime player, int count) //상자획득시 3개 선택지 아이템 중 랜덤하나
    {
        List<RewardOption> candidates = new();

        if (player != null)
        {
            AddItemCandidates(player, candidates);
        }

        if (candidates.Count == 0 && player != null)
        {
            candidates.Add(RewardOption.HealReward(Mathf.Max(1f, player.Health.MaxHp * 0.25f)));
        }

        return PickRandom(candidates, Mathf.Max(0, count));
    }



    private void AddOwnedWeaponUpgradeCandidates(PlayerRuntime player, List<RewardOption> candidates)
    {
        foreach (WeaponInstance weapon in player.Inventory.Weapons)
        {
            if (weapon != null && IsValidOwnedWeaponReward(weapon))
                candidates.Add(RewardOption.WeaponReward(weapon.Definition, false, weapon.CurrentLevel + 1));
        }
    }

    private void AddOwnedPassiveUpgradeCandidates(PlayerRuntime player, List<RewardOption> candidates)
    {
        foreach (PassiveInstance passive in player.Inventory.Passives)
        {
            if (passive != null && !passive.IsMaxLevel)
                candidates.Add(RewardOption.PassiveReward(passive.Definition, false, passive.CurrentLevel + 1));
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
            if (IsValidNewWeaponReward(weapon) && !HasWeapon(player, weapon.WeaponId))
                candidates.Add(RewardOption.WeaponReward(weapon, true, 1));
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
                candidates.Add(RewardOption.PassiveReward(passive, true, 1));
        }
    }
    private void AddItemCandidates(PlayerRuntime player, List<RewardOption> candidates)
    {
        if (itemTable == null)
            return;

        if (!player.Inventory.CanAddItme())
            return;

        foreach (ItemDefinitionSO item in itemTable.Items)
        {
            if (item != null)
            {
                candidates.Add(RewardOption.ItemReward(item));
            }
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

    private static bool IsValidOwnedWeaponReward(WeaponInstance weapon)
    {
        return weapon.Definition != null &&
               weapon.Definition.MaxLevel > 0 &&
               !weapon.IsMaxLevel;
    }

    private static bool IsValidNewWeaponReward(WeaponDefinitionSO weapon)
    {
        return weapon != null &&
               weapon.MaxLevel > 0 &&
               weapon.WeaponPrefab != null;
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
