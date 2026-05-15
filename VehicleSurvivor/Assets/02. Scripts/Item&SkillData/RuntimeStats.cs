using UnityEngine;

[System.Serializable]
public struct RuntimeStats
{
    public float MaxHealth;
    public float Defense;

    public float MoveSpeedMultiplier;
    public float DamageMultiplier;
    public float PickupRangeMultiplier;

    public float CooldownReduction;
    public float CritChance;
    public float CritDamageMultiplier;
    public float SkillSizeMultiplier;
    public float ExpGainMultiplier;

    public int ProjectileBonus;

    public static RuntimeStats FromCharacter(CharacterDefinitionSO character)
    {
        CharacterCombatStats combat = character.Combat;

        return new RuntimeStats
        {
            MaxHealth = combat.MaxHealth,
            Defense = combat.Defense,

            MoveSpeedMultiplier = combat.MoveSpeedMultiplier,
            DamageMultiplier = combat.DamageMultiplier,
            PickupRangeMultiplier = combat.PickupRange,

            CooldownReduction = 0f,
            CritChance = 0f,
            CritDamageMultiplier = 1.5f,
            SkillSizeMultiplier = 1f,
            ExpGainMultiplier = 1f,

            ProjectileBonus = 0,
        };
    }
}