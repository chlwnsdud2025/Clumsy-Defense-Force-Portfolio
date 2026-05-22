using System;

public sealed class LevelRuntime
{
    public int Level { get; private set; }
    public int CurrentExp { get; private set; }
    public int RequiredExp { get; private set; }

    public event Action<int, int> OnExpChanged;
    public event Action<int, int> OnLevelChanged;

    public LevelRuntime()
    {
        Level = 1;
        CurrentExp = 0;
        RequiredExp = CalculateRequiredExp(Level);
    }

    public int AddExp(int amount)
    {
        if (amount <= 0)
            return 0;

        int startLevel = Level;
        CurrentExp += amount;

        while (CurrentExp >= RequiredExp)
        {
            CurrentExp -= RequiredExp;
            Level++;
            RequiredExp = CalculateRequiredExp(Level);
        }

        int levelsGained = Level - startLevel;

        OnExpChanged?.Invoke(CurrentExp, RequiredExp);

        if (levelsGained > 0)
            OnLevelChanged?.Invoke(Level, levelsGained);

        return levelsGained;
    }

    private int CalculateRequiredExp(int level)
    {
        return 10 + (level - 1) * 5;
    }
}
