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
        if (!ShouldUpdateGameplay)
            return;

        RunTime += deltaTime;
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

    private void ChangeState(ERunState nextState)
    {
        if (State == nextState)
            return;

        ERunState previousState = State;
        State = nextState;

        OnStateChanged?.Invoke(previousState, nextState);
    }
}
