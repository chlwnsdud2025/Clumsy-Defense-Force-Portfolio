using System;
using UnityEngine;

public sealed class LevelRuntime
{
    public int Level { get; private set; }
    public int CurrentExp { get; private set; }
    public int RequiredExp { get; private set; }

    public event Action<int, int> OnExpChanged;
    public event Action<int> OnLevelUp;

    public LevelRuntime()
    {
        Level = 1;
        CurrentExp = 0;
        RequiredExp = CalculateRequiredExp(Level);
    }

    public void AddExp(int amount)
    {
        if (amount <= 0)
            return;

        CurrentExp += amount;

        while (CurrentExp >= RequiredExp)
        {
            CurrentExp -= RequiredExp;
            Level++;

            RequiredExp = CalculateRequiredExp(Level);

            Debug.Log($"[LevelRuntime] LevelUp. Level={Level}, CurrentExp={CurrentExp}, RequiredExp={RequiredExp}, HasListeners={OnLevelUp != null}");
            OnLevelUp?.Invoke(Level);
        }

        OnExpChanged?.Invoke(CurrentExp, RequiredExp);
    }

    private int CalculateRequiredExp(int level)
    {
        return 10 + (level - 1) * 5;
    }
}
