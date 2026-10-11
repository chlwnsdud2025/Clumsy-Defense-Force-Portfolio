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

    // 2차 곡선 (현재 레벨 → 다음 레벨 필요량): Lv1 10 · Lv10 146 · Lv20 450 · Lv30 914 · Lv45 1910
    // 목표 진행도: P1 종료 Lv12~15, P3 종료 Lv28~32, P6 종료 Lv45~55
    private int CalculateRequiredExp(int level)
    {
        int n = level - 1;
        return 10 + n * 8 + n * n * 4 / 5;
    }
}
