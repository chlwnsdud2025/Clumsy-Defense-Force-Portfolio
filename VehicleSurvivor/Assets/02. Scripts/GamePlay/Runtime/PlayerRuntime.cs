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
        Stats.SetBaseStat(EStatType.HealthRegen, 1f);
        Stats.SetBaseStat(EStatType.Defense, combat.Defense);

        Stats.SetBaseStat(EStatType.MoveSpeedMultiplier, combat.MoveSpeedMultiplier);
        Stats.SetBaseStat(EStatType.DamageMultiplier, combat.DamageMultiplier);
        Stats.SetBaseStat(EStatType.PickupRangeMultiplier, combat.PickupRange);

        Stats.SetBaseStat(EStatType.MagicDamageMultiplier, 1f);
        Stats.SetBaseStat(EStatType.EvolvedWeaponDamage, 1f);

        Stats.SetBaseStat(EStatType.CooldownReduction, 0f);
        Stats.SetBaseStat(EStatType.CriticalChance, 0f);
        Stats.SetBaseStat(EStatType.CriticalDamageMultiplier, 1.5f);
        Stats.SetBaseStat(EStatType.WeaponSizeMultiplier, 1f);
        Stats.SetBaseStat(EStatType.ProjectileCount, 0f);

        Stats.SetBaseStat(EStatType.ExpGainMultiplier, 1f);
        Stats.SetBaseStat(EStatType.GoldGainMultiplier, 1f);
        Stats.SetBaseStat(EStatType.Luck, 0f);
    }
}
