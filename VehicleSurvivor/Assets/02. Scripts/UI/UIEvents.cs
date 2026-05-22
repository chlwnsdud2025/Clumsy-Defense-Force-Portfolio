public readonly struct HpChangedEvent
{
    public readonly int Current;
    public readonly int Max;

    public HpChangedEvent(int current, int max)
    {
        Current = current;
        Max = max;
    }
}

public readonly struct KillCountEvent
{
    public readonly int KillCount;

    public KillCountEvent(int killcount)
    {
        KillCount = killcount;
    }
}

public readonly struct TimeChangeEvent
{
    public readonly float Time;

    public TimeChangeEvent(float time)
    {
        Time = time;
    }
}

public readonly struct WaveTimeChangedEvent
{
    public readonly float DisplayTime;
    public readonly bool IsNightmare;

    public WaveTimeChangedEvent(float displayTime, bool isNightmare)
    {
        DisplayTime = displayTime;
        IsNightmare = isNightmare;
    }
}

public readonly struct GoldChangedEvent
{
    public readonly int Gold;

    public GoldChangedEvent(int gold)
    {
        Gold = gold;
    }
}

public readonly struct ExpChangedEvent
{
    public readonly int Current;
    public readonly int Required;

    public ExpChangedEvent(int current, int required)
    {
        Current = current;
        Required = required;
    }
}

public readonly struct LevelChangedEvent
{
    public readonly int Level;

    public LevelChangedEvent(int level)
    {
        Level = level;
    }
}
