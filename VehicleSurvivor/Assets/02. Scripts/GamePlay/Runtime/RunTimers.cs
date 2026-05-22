using UnityEngine;

public sealed class RunTimer
{
    public float Elapsed { get; private set; }

    public void Reset()
    {
        Elapsed = 0f;
    }

    public void Tick(float deltaTime)
    {
        Elapsed += deltaTime;
    }
}

public sealed class WavePhaseTimer
{
    public float PhaseTimeLeft { get; private set; }
    public float PhaseElapsed { get; private set; }
    public float NightmareElapsed { get; private set; }
    public bool IsNightmare { get; private set; }

    public void Reset()
    {
        PhaseTimeLeft = 0f;
        PhaseElapsed = 0f;
        NightmareElapsed = 0f;
        IsNightmare = false;
    }

    public void StartPhase(float timeLimit)
    {
        PhaseTimeLeft = timeLimit;
        PhaseElapsed = 0f;
        NightmareElapsed = 0f;
        IsNightmare = false;
    }

    public void Tick(float deltaTime)
    {
        PhaseTimeLeft -= deltaTime;

        if (PhaseTimeLeft > 0f)
        {
            PhaseElapsed += deltaTime;
            return;
        }

        if (!IsNightmare)
        {
            IsNightmare = true;
            NightmareElapsed = 0f;
            Debug.LogWarning("제한 시간 초과! 악몽 웨이브 시작!");
        }

        NightmareElapsed += deltaTime;
    }
}

public static class WaveSelection
{
    public static WaveEnemy_DataSO GetCurrentWave(StagePhase phase, WavePhaseTimer timer)
    {
        if (phase == null || timer == null)
            return null;

        WaveEnemy_DataSO activeWave = null;

        if (timer.IsNightmare)
        {
            if (phase.nightmareWaves == null)
                return null;

            foreach (WaveEnemy_DataSO wave in phase.nightmareWaves)
            {
                if (wave != null && wave.waveStartTime <= timer.NightmareElapsed)
                    activeWave = wave;
            }
        }
        else
        {
            if (phase.phaseWaves == null)
                return null;

            foreach (WaveEnemy_DataSO wave in phase.phaseWaves)
            {
                if (wave != null && wave.waveStartTime <= timer.PhaseElapsed)
                    activeWave = wave;
            }
        }

        return activeWave;
    }
}
