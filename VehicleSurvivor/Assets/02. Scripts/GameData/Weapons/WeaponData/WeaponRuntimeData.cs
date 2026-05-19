public class WeaponRuntimeData
{
    public WeaponDefinitionSO Definition { get; }
    public int CurrentLevel { get; private set; }

    public int MaxLevel => Definition.MaxLevel;
    public bool IsMaxLevel => CurrentLevel >= MaxLevel;

    public WeaponLevelData CurrentLevelData => Definition.GetLevelData(CurrentLevel);

    public WeaponRuntimeData(WeaponDefinitionSO definition)
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