using UnityEngine;

public enum ERewardType
{
    Weapon,
    Passive,
    Item,
    Attack, // 수치를 적게 올리기? , 쿨타임, 치명타, 방어력, 체력 등등?
    Heal
}

public sealed class RewardOption
{
    public ERewardType Type { get; }
    public WeaponDefinitionSO Weapon { get; }
    public PassiveDefinitionSO Passive { get; }
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

            if (Passive != null)
                return Passive.PassiveName;

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

            if (Passive != null)
                return BuildPassiveDescription(Passive, Level);

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
            if (Passive != null)
                return Passive.Icon;
            if (Item != null)
                return Item.Icon;

            return null;
        }
    }
    private RewardOption(ERewardType type, WeaponDefinitionSO weapon, PassiveDefinitionSO passive, ItemDefinitionSO item, float healAmount, bool isNew, int level)
    {
        Type = type;
        Weapon = weapon;
        Passive = passive;
        Item = item;
        HealAmount = healAmount;
        IsNew = isNew;
        Level = level;
    }

    public static RewardOption WeaponReward(WeaponDefinitionSO weapon, bool isNew, int level)
    {
        return new RewardOption(ERewardType.Weapon, weapon, null, null, 0f, isNew, level);
    }

    public static RewardOption PassiveReward(PassiveDefinitionSO passive, bool isNew, int level)
    {
        return new RewardOption(ERewardType.Passive, null, passive, null, 0f, isNew, level);
    }

    public static RewardOption ItemReward(ItemDefinitionSO item)
    {
        return new RewardOption(ERewardType.Item, null, null, item, 0f, true, 1);
    }

    public static RewardOption HealReward(float amount)
    {
        return new RewardOption(ERewardType.Heal, null, null, null, amount, false, 0);
    }

    private static string BuildPassiveDescription(PassiveDefinitionSO passive, int level)
    {
        if (passive == null)
            return string.Empty;

        string template = passive.Description;
        StatModifierData modifier = passive.GetModifierData(level);

        if (modifier == null || string.IsNullOrWhiteSpace(template))
            return template ?? string.Empty;

        if (modifier.Value == 0f)
            return "이번 레벨에는 추가 효과가 없습니다.";

        if (!template.Contains("{0}"))
            return template;

        string valueText = FormatModifierValue(modifier);
        if (level > 1)
        {
            StatModifierData previousModifier = passive.GetModifierData(level - 1);
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
