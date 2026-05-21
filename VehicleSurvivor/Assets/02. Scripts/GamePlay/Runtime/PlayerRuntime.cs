public sealed class PlayerRuntime //이번 판의 플레이어 상태 묶음
{
    public CharacterDefinitionSO Character { get; }

    public RuntimeStats Stats { get; }
    public PlayerHealth Health { get; }
    public LevelRuntime Level { get; }
    public PlayerInventory Inventory { get; }

    public PlayerRuntime(CharacterDefinitionSO character)
    {
        Character = character;

        Stats = new RuntimeStats();
        InitializeBaseStats(character);

        Health = new PlayerHealth(Stats);
        Level = new LevelRuntime();
        Inventory = new PlayerInventory(this);
    }

    private void InitializeBaseStats(CharacterDefinitionSO character)
    {
        CharacterCombatStats combat = character.Combat;

        Stats.SetBaseStat(EStatType.MaxHealth, combat.MaxHealth);
        Stats.SetBaseStat(EStatType.Defense, combat.Defense);

        Stats.SetBaseStat(EStatType.MoveSpeedMultiplier, combat.MoveSpeedMultiplier);
        Stats.SetBaseStat(EStatType.DamageMultiplier, combat.DamageMultiplier);
        Stats.SetBaseStat(EStatType.PickupRangeMultiplier, combat.PickupRange);

        Stats.SetBaseStat(EStatType.CooldownReduction, 0f);
        Stats.SetBaseStat(EStatType.CritChance, 0f);
        Stats.SetBaseStat(EStatType.CritDamageMultiplier, 1.5f);
        Stats.SetBaseStat(EStatType.SkillSizeMultiplier, 1f);
        Stats.SetBaseStat(EStatType.ExpGainMultiplier, 1f);

        Stats.SetBaseStat(EStatType.ProjectileCount, 0f);
    }
}
