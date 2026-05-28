public sealed class ItemInstance
{
    public ItemDefinitionSO Definition { get; }
    public int StackCount { get; private set; }

    private readonly PlayerRuntime owner;

    public ItemInstance(ItemDefinitionSO definition, PlayerRuntime owner)
    {
        Definition = definition;
        this.owner = owner;
        StackCount = 1;

        ApplyCurrentModifiers();
    }

    public void AddStack()
    {
        StackCount++;
        ApplyCurrentModifiers();
    }

    private void ApplyCurrentModifiers()
    {
        if (owner == null || Definition == null)
            return;

        owner.Stats.RemoveModifiersFrom(this);

        foreach (StatModifierData modifier in Definition.StatModifiers)
        {
            if (modifier == null || modifier.StatType == EStatType.None)
                continue;

            EStatModifierType modifierType = modifier.IsPercent
                ? EStatModifierType.Percent
                : EStatModifierType.Flat;

            owner.Stats.AddModifier(
                this,
                modifier.StatType,
                modifierType,
                modifier.Value * StackCount);
        }
    }
}
