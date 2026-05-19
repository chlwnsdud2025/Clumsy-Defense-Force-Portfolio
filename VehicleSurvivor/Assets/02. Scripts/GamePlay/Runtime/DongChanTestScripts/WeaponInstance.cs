public sealed class WeaponInstance
{
    public WeaponDefinitionSO Definition { get; }
    public int CurrentLevel { get; private set; }
    public float CurrentCooldown { get; private set; }

    public int MaxLevel => Definition.MaxLevel;
    public bool IsMaxLevel => CurrentLevel >= MaxLevel;
    public bool IsReady => CurrentCooldown <= 0f;

    public WeaponLevelData CurrentLevelData => Definition.GetLevelData(CurrentLevel);

    public WeaponInstance(WeaponDefinitionSO definition)
    {
        Definition = definition;
        CurrentLevel = 1;
        CurrentCooldown = 0f;
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

    public void TickCooldown(float deltaTime)
    {
        if (CurrentCooldown <= 0f)
        {
            return;
        }

        CurrentCooldown -= deltaTime;

        if (CurrentCooldown < 0f)
        {
            CurrentCooldown = 0f;
        }
    }

    public void ResetCooldown()
    {
        WeaponLevelData levelData = CurrentLevelData;
        CurrentCooldown = levelData != null ? levelData.Cooldown : 0f;
    }
}
