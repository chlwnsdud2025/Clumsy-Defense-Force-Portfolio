using System;
using UnityEngine;

public sealed class RunSession
{
    private readonly RunTimer _runTimer = new();
    private readonly WavePhaseTimer _waveTimer = new();

    private int _killCount;
    private int _gold;

    private int _lastDisplayedRunSecond = -1;
    private int _lastDisplayedWaveSecond = -1;

    public ERunState State { get; private set; }
    public PlayerRuntime Player { get; }

    public bool ShouldUpdateGameplay => State == ERunState.Playing;
    public bool IsElapsedTime => State == ERunState.RewardSelection;
    public bool IsPaused => State == ERunState.Paused;
    public bool IsFinished =>
        State == ERunState.GameOver ||
        State == ERunState.Cleared ||
        State == ERunState.Ended;

    public float RunTime => _runTimer.Elapsed;
    public WavePhaseTimer WaveTimer => _waveTimer;
    public int KillCount => _killCount;
    public int Gold => _gold;

    public event Action<ERunState, ERunState> OnStateChanged;
    public event Action<float> OnRunTimeDisplayChanged;
    public event Action<float, bool> OnWaveTimeDisplayChanged;
    public event Action<int> OnKillCountChanged;
    public event Action<int> OnGoldChanged;

    public RunSession(PlayerRuntime player)
    {
        Player = player;
        State = ERunState.None;
    }

    public void ResetStats()
    {
        _killCount = 0;
        _gold = 0;
        _runTimer.Reset();
        _waveTimer.Reset();
        ResetDisplayTracking();
    }

    public void Tick(float deltaTime)
    {
        if (!ShouldUpdateGameplay)
            return;

        bool wasNightmare = _waveTimer.IsNightmare;
        _runTimer.Tick(deltaTime);
        _waveTimer.Tick(deltaTime);

        if (_waveTimer.IsNightmare && !wasNightmare)
            _lastDisplayedWaveSecond = -1;

        TryEmitRunTimeDisplay();
        TryEmitWaveTimeDisplay();
    }

    public void FlushTimerDisplay()
    {
        ResetDisplayTracking();
        TryEmitRunTimeDisplay(force: true);
        TryEmitWaveTimeDisplay(force: true);
    }

    public float GetWaveDisplayTime()
    {
        return _waveTimer.IsNightmare
            ? _waveTimer.NightmareElapsed
            : Mathf.Max(0f, _waveTimer.PhaseTimeLeft);
    }

    public bool TryAddKill()
    {
        if (!ShouldUpdateGameplay)
            return false;

        _killCount++;
        OnKillCountChanged?.Invoke(_killCount);
        return true;
    }

    public bool TryAddGold(int amount)
    {
        if (!ShouldUpdateGameplay)
            return false;

        _gold += amount;
        OnGoldChanged?.Invoke(_gold);
        return true;
    }

    public void StartWavePhase(float timeLimit)
    {
        _waveTimer.StartPhase(timeLimit);
        _lastDisplayedWaveSecond = -1;
        TryEmitWaveTimeDisplay(force: true);
    }

    public void StartRun()
    {
        ChangeState(ERunState.Playing);
    }

    public void EnterRewardSelection()
    {
        if (State != ERunState.Playing)
            return;

        ChangeState(ERunState.RewardSelection);
    }

    public void ResumeGamePlay()
    {
        if (State != ERunState.RewardSelection &&
            State != ERunState.Paused)
            return;

        ChangeState(ERunState.Playing);
    }

    public void Pause()
    {
        if (State != ERunState.Playing)
            return;

        ChangeState(ERunState.Paused);
    }

    public void GameOver()
    {
        if (IsFinished)
            return;

        ChangeState(ERunState.GameOver);
    }

    public void Clear()
    {
        if (IsFinished)
            return;

        ChangeState(ERunState.Cleared);
    }

    public void EndRun()
    {
        if (State == ERunState.Ended)
            return;

        ChangeState(ERunState.Ended);
    }

    private void ResetDisplayTracking()
    {
        _lastDisplayedRunSecond = -1;
        _lastDisplayedWaveSecond = -1;
    }

    private void TryEmitRunTimeDisplay(bool force = false)
    {
        int currentRunSecond = Mathf.FloorToInt(_runTimer.Elapsed);
        if (!force && currentRunSecond == _lastDisplayedRunSecond)
            return;

        _lastDisplayedRunSecond = currentRunSecond;
        OnRunTimeDisplayChanged?.Invoke(_runTimer.Elapsed);
    }

    private void TryEmitWaveTimeDisplay(bool force = false)
    {
        int currentWaveSecond = _waveTimer.IsNightmare
            ? Mathf.FloorToInt(_waveTimer.NightmareElapsed)
            : Mathf.CeilToInt(Mathf.Max(0f, _waveTimer.PhaseTimeLeft));

        if (!force && currentWaveSecond == _lastDisplayedWaveSecond)
            return;

        _lastDisplayedWaveSecond = currentWaveSecond;
        OnWaveTimeDisplayChanged?.Invoke(GetWaveDisplayTime(), _waveTimer.IsNightmare);
    }

    private void ChangeState(ERunState nextState)
    {
        if (State == nextState)
            return;

        ERunState previousState = State;
        State = nextState;

        OnStateChanged?.Invoke(previousState, nextState);
    }
}
