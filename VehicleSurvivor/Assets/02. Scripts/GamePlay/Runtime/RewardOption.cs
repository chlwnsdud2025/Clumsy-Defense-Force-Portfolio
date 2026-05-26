using UnityEngine;

public enum ERewardType
{
    Weapon,
    Badge,
    Item,
    Attack, // ?˜ì¹˜ë¥??ê²Œ ?¬ë¦¬ê¸? , ì¿¨í??? ì¹˜ëª…?€, ë°©ì–´?? ì²´ë ¥ ?±ë“±?
    Heal
}

public sealed class RewardOption
{
    public ERewardType Type { get; }
    public WeaponDefinitionSO Weapon { get; }
    public BadgeDefinitionSO Badge { get; }
    public ItemDefinitionSO Item { get; }
    public float HealAmount { get; }
    public bool IsNew { get; }
    public int Level { get; }

    public EItemRarity ItemRarity => Item?.Rarity ?? EItemRarity.None;

    public string DisplayName
    {
        get
        {
            if (Weapon != null)
                return Weapon.WeaponName;

            if (Badge != null)
                return Badge.BadgeName;

            if (Item != null)
                return Item.ItemName;


            return "Heal";
        }
    }

    public string Description
    {
        get
        {
            if (Weapon != null)
            {
                if (IsNew)
                    return Weapon.Description;

                WeaponLevelData levelData = Weapon.GetLevelData(Level);
                return levelData != null && !string.IsNullOrWhiteSpace(levelData.Description)
                    ? levelData.Description
                    : Weapon.Description;
            }

            if (Badge != null)
                return BuildBadgeDescription(Badge, Level);

            if (Item != null)
                return Item.Description;

            return $"+{Mathf.RoundToInt(HealAmount)} HP";
        }
    }

    public Sprite Icon
    {
        get
        {
            if (Weapon != null)
                return Weapon.Icon;
            if (Badge != null)
                return Badge.Icon;
            if (Item != null)
                return Item.Icon;

            return null;
        }
    }
    private RewardOption(ERewardType type, WeaponDefinitionSO weapon, BadgeDefinitionSO badge, ItemDefinitionSO item, float healAmount, bool isNew, int level)
    {
        Type = type;
        Weapon = weapon;
        Badge = badge;
        Item = item;
        HealAmount = healAmount;
        IsNew = isNew;
        Level = level;
    }

    public static RewardOption WeaponReward(WeaponDefinitionSO weapon, bool isNew, int level)
    {
        return new RewardOption(ERewardType.Weapon, weapon, null, null, 0f, isNew, level);
    }

    public static RewardOption BadgeReward(BadgeDefinitionSO badge, bool isNew, int level)
    {
        return new RewardOption(ERewardType.Badge, null, badge, null, 0f, isNew, level);
    }

    public static RewardOption ItemReward(ItemDefinitionSO item)
    {
        return new RewardOption(ERewardType.Item, null, null, item, 0f, true, 1);
    }

    public static RewardOption HealReward(float amount)
    {
        return new RewardOption(ERewardType.Heal, null, null, null, amount, false, 0);
    }

    private static string BuildBadgeDescription(BadgeDefinitionSO badge, int level)
    {
        if (badge == null)
            return string.Empty;

        string template = badge.Description;
        StatModifierData modifier = badge.GetModifierData(level);

        if (modifier == null || string.IsNullOrWhiteSpace(template))
            return template ?? string.Empty;

        if (modifier.Value == 0f)
            return "?´ë²ˆ ?ˆë²¨?ëŠ” ì¶”ê? ?¨ê³¼ê°€ ?†ìŠµ?ˆë‹¤.";

        if (!template.Contains("{0}"))
            return template;

        string valueText = FormatModifierValue(modifier);
        if (level > 1)
        {
            StatModifierData previousModifier = badge.GetModifierData(level - 1);
            if (previousModifier != null)
                valueText = $"{FormatModifierValue(previousModifier)} > {valueText}";
        }

        return string.Format(template, valueText);
    }

    private static string FormatModifierValue(StatModifierData modifier)
    {
        if (modifier.IsPercent)
            return $"{Mathf.RoundToInt(modifier.Value * 100f)}%";

        return Mathf.Approximately(modifier.Value, Mathf.Round(modifier.Value))
            ? Mathf.RoundToInt(modifier.Value).ToString()
            : modifier.Value.ToString("0.##");
    }
}
