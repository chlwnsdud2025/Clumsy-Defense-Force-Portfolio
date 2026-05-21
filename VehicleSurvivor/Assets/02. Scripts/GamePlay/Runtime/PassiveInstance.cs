public sealed class PassiveInstance
{
    public PassiveDefinitionSO Definition { get; }
    public int CurrentLevel { get; private set; }

    private readonly PlayerRuntime owner;

    public int MaxLevel => Definition.MaxLevel;
    public bool IsMaxLevel => CurrentLevel >= MaxLevel;

    public StatModifierData CurrentModifierData => Definition.GetModifierData(CurrentLevel);

    public PassiveInstance(PassiveDefinitionSO definition, PlayerRuntime owner)
    {
        Definition = definition;
        this.owner = owner;
        CurrentLevel = 1;

        ApplyCurrentModifier();
    }

    public bool TryLevelUp()
    {
        if (IsMaxLevel)
        {
            return false;
        }

        CurrentLevel++;
        ApplyCurrentModifier();
        return true;
    }

    private void ApplyCurrentModifier()
    {
        if (owner == null)
        {
            return;
        }

        StatModifierData modifier = CurrentModifierData;
        if (modifier == null)
        {
            return;
        }

        EStatType statType = modifier.StatType != EStatType.None
            ? modifier.StatType
            : Definition.StatType;

        if (statType == EStatType.None)
        {
            return;
        }

        EStatModifierType modifierType = modifier.IsPercent
            ? EStatModifierType.Percent
            : EStatModifierType.Flat;

        owner.Stats.ReplaceModifier(this, statType, modifierType, modifier.Value);
    }
}
