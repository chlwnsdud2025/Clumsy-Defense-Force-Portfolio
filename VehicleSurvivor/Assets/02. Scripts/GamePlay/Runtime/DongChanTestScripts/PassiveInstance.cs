public sealed class PassiveInstance
{
    public PassiveDefinitionSO Definition { get; }
    public int CurrentLevel { get; private set; }

    public int MaxLevel => Definition.MaxLevel;
    public bool IsMaxLevel => CurrentLevel >= MaxLevel;

    public StatModifierData CurrentModifierData => Definition.GetModifierData(CurrentLevel);

    public PassiveInstance(PassiveDefinitionSO definition)
    {
        Definition = definition;
        CurrentLevel = 1;
    }

    public bool TryLevelUp()
    {
        if (IsMaxLevel)
        {
            return false;
        }

        CurrentLevel++;
        return true;
    }
}
