using UnityEngine;

public enum ERewardType
{
    Weapon,
    Passive,
    Heal
}

public sealed class RewardOption
{
    public ERewardType Type { get; }
    public WeaponDefinitionSO Weapon { get; }
    public PassiveDefinitionSO Passive { get; }
    public float HealAmount { get; }

    public string DisplayName
    {
        get
        {
            if (Weapon != null)
                return Weapon.WeaponName;

            if (Passive != null)
                return Passive.PassiveName;

            return "Heal";
        }
    }

    public string Description
    {
        get
        {
            if (Weapon != null)
                return Weapon.Description;

            if (Passive != null)
                return Passive.Description;

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

            return null;
        }
    }

    private RewardOption(ERewardType type, WeaponDefinitionSO weapon, PassiveDefinitionSO passive, float healAmount)
    {
        Type = type;
        Weapon = weapon;
        Passive = passive;
        HealAmount = healAmount;
    }

    public static RewardOption WeaponReward(WeaponDefinitionSO weapon)
    {
        return new RewardOption(ERewardType.Weapon, weapon, null, 0f);
    }

    public static RewardOption PassiveReward(PassiveDefinitionSO passive)
    {
        return new RewardOption(ERewardType.Passive, null, passive, 0f);
    }

    public static RewardOption HealReward(float amount)
    {
        return new RewardOption(ERewardType.Heal, null, null, amount);
    }
}
