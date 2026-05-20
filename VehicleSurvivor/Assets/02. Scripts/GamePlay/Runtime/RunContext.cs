using System;

public sealed class RunContext
{
    public ERunState State { get; private set; }
    public float RunTime { get; private set; }
    public PlayerRuntime Player { get; }

    public bool ShouldUpdateGameplay => State == ERunState.Playing;
    public bool IsElapsedTime => State == ERunState.RewardSelection;
    public bool IsPaused => State == ERunState.Paused;
    public bool IsFinished => 
        State == ERunState.GameOver ||
        State == ERunState.Cleared ||
        State == ERunState.Ended;

    public event Action<ERunState, ERunState> OnStateChanged;

    public RunContext(PlayerRuntime player)
    {
        Player = player;
        State = ERunState.None;
        RunTime = 0f;
    }
    public void StartRun()
    {
        ChangeState(ERunState.Playing);
    }
    public void Tick(float deltaTime)
    {
        if (!ShouldUpdateGameplay) return;

        RunTime += deltaTime;
    }
    public void EnterRewardSelection()
    {
        if (State != ERunState.Playing) return;

        ChangeState(ERunState.RewardSelection);
    }

    public void ResumeGamePlay()
    {
        if (State != ERunState.RewardSelection &&
            State != ERunState.Paused) return;
        ChangeState(ERunState.Playing);
    }
    public void Pause()
    {
        if (State != ERunState.Playing) return;

        ChangeState(ERunState.Paused);
    }
    public void GameOver()
    {
        if (IsFinished) return;

        ChangeState(ERunState.GameOver);
    }
    public void Clear()
    {
        if (IsFinished) return;

        ChangeState(ERunState.Cleared);
    }
    public void EndRun()
    {
        if (State == ERunState.Ended) return;

        ChangeState(ERunState.Ended);
    }


    private void ChangeState(ERunState nextState)
    {
        if (State == nextState) return;

        ERunState previousState = State;
        State = nextState;

        OnStateChanged?.Invoke(previousState, nextState);
    }


    ////<기존 코드>
    //public CharacterDefinitionSO Character { get; }

    //public RuntimeStats Stats { get; private set; }

    //public int CurrentHp { get; private set; }
    //public int MaxHp => Mathf.RoundToInt(Stats.MaxHealth);

    //private readonly Dictionary<string, int> _passiveLevels = new();
    //private readonly HashSet<string> _ownedItems = new();

    //public IReadOnlyDictionary<string, int> PassiveLevels => _passiveLevels;
    //public IReadOnlyCollection<string> OwnedItems => _ownedItems;

    //public RunContext(CharacterDefinitionSO character)
    //{
    //    Character = character;
    //    Stats = RuntimeStats.FromCharacter(character);
    //    CurrentHp = MaxHp;
    //}

    //public bool HasPassive(string passiveId)
    //{
    //    return !string.IsNullOrWhiteSpace(passiveId)
    //        && _passiveLevels.ContainsKey(passiveId);
    //}
    //public int GetPassiveLevel(string passiveId)
    //{
    //    if(string.IsNullOrWhiteSpace(passiveId))
    //        return 0;
    //    return _passiveLevels.TryGetValue(passiveId, out int level) ? level : 0;
    //}
   
    //public void AddPassiveLevel(string passiveId, int amount = 1)
    //{
    //    if (string.IsNullOrWhiteSpace(passiveId))
    //        return;
    //    int currentLevel = GetPassiveLevel(passiveId);
    //    _passiveLevels[passiveId] = Mathf.Max(0, currentLevel + amount);
    //    //_passiveLevels[passiveId] = level;
    //}
    //public void SetPassiveLevel(string passiveId, int level)
    //{
    //    if (string.IsNullOrWhiteSpace(passiveId))
    //        return;

    //    _passiveLevels[passiveId] = Mathf.Max(0, level);
    //}
    //public bool AddOwnedItem(string itemId)
    //{
    //    if (string.IsNullOrWhiteSpace(itemId))
    //        return false;

    //    return _ownedItems.Add(itemId);
    //}

    //public void ApplyStats(RuntimeStats stats)
    //{
    //    Stats = stats;

    //    if (CurrentHp > MaxHp)
    //        CurrentHp = MaxHp;
    //}

    //public void Heal(int amount)
    //{
    //    CurrentHp = Mathf.Min(CurrentHp + amount, MaxHp);
    //}

    //public void TakeDamage(int amount)
    //{
    //    CurrentHp = Mathf.Max(0, CurrentHp - amount);
    //}
}
