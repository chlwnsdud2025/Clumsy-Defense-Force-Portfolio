using System.Collections.Generic;
using UnityEngine;

public sealed class RewardSystem
{
    private readonly WeaponDatabaseSO weaponDatabase;
    private readonly BadgeTableSO badgeTable;
    private readonly ItemTableSO itemTable;
    private const float DefaultRewardWeight = 1f;
    private const float OwnedWeaponUpgradeWeight = 3f;
    private static readonly Dictionary<EItemRarity, float> ChestRarityWeights = new()
    {
        { EItemRarity.Common, 60f },
        { EItemRarity.Uncommon, 25f },
        { EItemRarity.Rare, 10f },
        { EItemRarity.Epic, 4f },
        { EItemRarity.Legendary, 0.9f },
        { EItemRarity.Mythic, 0.1f },
    };

    public RewardSystem(WeaponDatabaseSO weaponDatabase, BadgeTableSO badgeTable, ItemTableSO itemTable)
    {
        this.weaponDatabase = weaponDatabase;
        this.badgeTable = badgeTable;
        this.itemTable = itemTable;
    }
    
    public List<RewardOption> GenerateLevelUp(PlayerRuntime player, int count) //?àÎ≤®?ÖÏãú 3Í∞??†ÌÉùÏßÄ Î¨¥Í∏∞, ?®ÏãúÎ∏?Ï§ëÏóê ?úÎç§?òÎÇò
    {
        List<RewardOption> candidates = new();

        if (player != null)
        {
            AddOwnedWeaponUpgradeCandidates(player, candidates);
            AddOwnedBadgeUpgradeCandidates(player, candidates);
            AddNewWeaponCandidates(player, candidates);
            AddNewBadgeCandidates(player, candidates);
        }

        if (candidates.Count == 0 && player != null)
        {
            candidates.Add(RewardOption.HealReward(Mathf.Max(1f, player.Health.MaxHp * 0.25f)));
        }

        return PickRandomByRewardWeight(candidates, Mathf.Max(0, count));
    }

    public List<RewardOption> GenerateChest(PlayerRuntime player, int count) //?ÅÏûê?çÎìù??3Í∞??†ÌÉùÏßÄ ?ÑÏù¥??Ï§??úÎç§?òÎÇò
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

        return PickRandomByItemRarity(candidates, Mathf.Max(0, count), player.Stats.GetValue(EStatType.Luck));
    }



    private void AddOwnedWeaponUpgradeCandidates(PlayerRuntime player, List<RewardOption> candidates)
    {
        foreach (WeaponInstance weapon in player.Inventory.Weapons)
        {
            if (weapon != null && IsValidOwnedWeaponReward(weapon))
                candidates.Add(RewardOption.WeaponReward(weapon.Definition, false, weapon.CurrentLevel + 1));
        }
    }

    private void AddOwnedBadgeUpgradeCandidates(PlayerRuntime player, List<RewardOption> candidates)
    {
        foreach (BadgeInstance badge in player.Inventory.Badges)
        {
            if (badge != null && !badge.IsMaxLevel)
                candidates.Add(RewardOption.BadgeReward(badge.Definition, false, badge.CurrentLevel + 1));
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
            {
                candidates.Add(RewardOption.WeaponReward(weapon, true, 1));
            }
        }
    }

    private void AddNewBadgeCandidates(PlayerRuntime player, List<RewardOption> candidates)
    {
        if (badgeTable == null)
            return;

        if (!player.Inventory.CanAddBadge())
            return;

        foreach (BadgeDefinitionSO badge in badgeTable.Badges)
        {
            if (badge != null && !HasBadge(player, badge.BadgeId))
                candidates.Add(RewardOption.BadgeReward(badge, true, 1));
        }
    }
    private void AddItemCandidates(PlayerRuntime player, List<RewardOption> candidates)
    {
        if (itemTable == null)
            return;

        if (!player.Inventory.CanAddItme()) // ?ÑÏù¥???¨Î°Ø ÏµúÎ??∏Ï? ?ïÏù∏
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

    private static bool HasBadge(PlayerRuntime player, string badgeId)
    {
        foreach (BadgeInstance badge in player.Inventory.Badges)
        {
            if (badge != null && badge.Definition.BadgeId == badgeId)
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

    private static List<RewardOption> PickRandomByRewardWeight(List<RewardOption> candidates, int count)
    {
        List<RewardOption> pool = new(candidates);
        List<RewardOption> selected = new();

        while (selected.Count < count && pool.Count > 0)
        {
            int index = PickWeightedRewardIndex(pool);
            selected.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return selected;
    }

    private static int PickWeightedRewardIndex(List<RewardOption> pool)
    {
        float totalWeight = 0f;

        foreach (RewardOption reward in pool)
        {
            totalWeight += GetRewardWeight(reward);
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        for (int i = 0; i < pool.Count; i++)
        {
            currentWeight += GetRewardWeight(pool[i]);

            if (randomValue <= currentWeight)
                return i;
        }

        return pool.Count - 1;
    }

    private static float GetRewardWeight(RewardOption reward)
    {
        if (reward.Type == ERewardType.Weapon && !reward.IsNew && reward.Level > 1)
        {
            return OwnedWeaponUpgradeWeight;
        }

        return DefaultRewardWeight;
    }

    private static List<RewardOption> PickRandomByItemRarity(List<RewardOption> candidates, int count, float luck)
    {
        List<RewardOption> pool = new(candidates);
        List<RewardOption> selected = new();

        while (selected.Count < count && pool.Count > 0)
        {
            int index = PickWeightedItemIndex(pool, luck);

            if (index < 0)
            {
                index = Random.Range(0, pool.Count);
            }

            selected.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return selected;
    }

    private static int PickWeightedItemIndex(List<RewardOption> pool, float luck)
    {
        float totalWeight = 0f;

        foreach (KeyValuePair<EItemRarity, float> pair in ChestRarityWeights)
        {
            if (pair.Value > 0f && HasItemRarity(pool, pair.Key))
            {
                totalWeight += GetAdjustedChestRarityWeight(pair.Key, luck);
            }
        }

        if (totalWeight <= 0f)
            return -1;

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (KeyValuePair<EItemRarity, float> pair in ChestRarityWeights)
        {
            if (pair.Value <= 0f || !HasItemRarity(pool, pair.Key))
                continue;

            currentWeight += GetAdjustedChestRarityWeight(pair.Key, luck);

            if (randomValue <= currentWeight)
            {
                return PickRandomItemIndexByRarity(pool, pair.Key);
            }
        }

        return -1;
    }

    private static float GetAdjustedChestRarityWeight(EItemRarity rarity, float luck)
    {
        if (!ChestRarityWeights.TryGetValue(rarity, out float baseWeight) || baseWeight <= 0f)
            return 0f;

        if (luck == 0f)
            return baseWeight;

        int rarityTier = GetRarityTier(rarity);
        float luckShift = luck * 0.02f;

        // RareÎ•?Í∏∞Ï??ºÎ°ú ?Ä?±Í∏â?Ä Í∞ÄÏ§ëÏπò ?? Í≥†Îì±Í∏âÏ? ??
        float multiplier = Mathf.Max(0.01f, 1f + luckShift * (rarityTier - GetRarityTier(EItemRarity.Rare)));
        return baseWeight * multiplier;
    }

    private static int GetRarityTier(EItemRarity rarity)
    {
        return rarity switch
        {
            EItemRarity.Common => 0,
            EItemRarity.Uncommon => 1,
            EItemRarity.Rare => 2,
            EItemRarity.Epic => 3,
            EItemRarity.Legendary => 4,
            EItemRarity.Mythic => 5,
            _ => 0,
        };
    }

    private static bool HasItemRarity(List<RewardOption> pool, EItemRarity rarity)
    {
        foreach (RewardOption reward in pool)
        {
            if (reward.Item != null && reward.Item.Rarity == rarity)
                return true;
        }

        return false;
    }

    private static int PickRandomItemIndexByRarity(List<RewardOption> pool, EItemRarity rarity)
    {
        int matchCount = 0;

        foreach (RewardOption reward in pool)
        {
            if (reward.Item != null && reward.Item.Rarity == rarity)
            {
                matchCount++;
            }
        }

        if (matchCount == 0)
            return -1;

        int target = Random.Range(0, matchCount);

        for (int i = 0; i < pool.Count; i++)
        {
            RewardOption reward = pool[i];

            if (reward.Item == null || reward.Item.Rarity != rarity)
                continue;

            if (target == 0)
                return i;

            target--;
        }

        return -1;
    }
}
