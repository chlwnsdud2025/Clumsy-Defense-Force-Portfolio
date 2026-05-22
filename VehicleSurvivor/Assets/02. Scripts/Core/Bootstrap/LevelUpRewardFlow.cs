using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class LevelUpRewardFlow
{
    private int pendingLevelUpRewardCount;
    private UI_LevelUp activeLevelUpPopup;

    public void Reset()
    {
        pendingLevelUpRewardCount = 0;
        activeLevelUpPopup = null;
    }

    public void OnLevelChanged(RunContext runContext, int levelsGained, RunManager runManager)
    {
        if (runContext == null || !runContext.ShouldUpdateGameplay)
            return;

        if (levelsGained <= 0)
            return;

        pendingLevelUpRewardCount += levelsGained;
        BeginLevelUpRewardsAsync(runContext, runManager).Forget();
    }

    public void OnRewardApplied(RunManager runManager, RunContext runContext)
    {
        pendingLevelUpRewardCount--;

        if (pendingLevelUpRewardCount > 0)
        {
            PresentLevelUpPopup(runManager, runContext);
            return;
        }

        FinishLevelUpRewards(runContext);
    }

    public void OnRunFinished()
    {
        ClosePopupIfOpen();
        Reset();
    }

    private async UniTaskVoid BeginLevelUpRewardsAsync(RunContext runContext, RunManager runManager)
    {
        if (runContext == null || pendingLevelUpRewardCount <= 0)
            return;

        if (activeLevelUpPopup != null)
        {
            PresentLevelUpPopup(runManager, runContext);
            return;
        }

        if (runContext.State != ERunState.Playing)
            return;

        runContext.EnterRewardSelection();

        UIManager uiManager = UIManager.Instance;
        if (uiManager == null)
        {
            HandleLevelUpPopupFailure(runContext);
            return;
        }

        activeLevelUpPopup = await uiManager.ShowPopupAsync<UI_LevelUp>(EPopupUIType.LevelUp, remember: false);
        if (runManager.RunContext == null || runContext.State != ERunState.RewardSelection)
            return;

        if (activeLevelUpPopup == null)
        {
            HandleLevelUpPopupFailure(runContext);
            return;
        }

        PresentLevelUpPopup(runManager, runContext);
    }

    private void PresentLevelUpPopup(RunManager runManager, RunContext runContext)
    {
        if (activeLevelUpPopup == null || runContext == null || runManager == null)
            return;

        List<RewardOption> rewardOptions = runManager.GenerateLevelUpRewardOptions();
        if (rewardOptions.Count == 0)
            Debug.LogWarning("[LevelUpRewardFlow] LevelUp reward option list is empty.");

        activeLevelUpPopup.ShowRewards(
            runManager,
            rewardOptions,
            runContext.Player.Level.Level);
    }

    private void FinishLevelUpRewards(RunContext runContext)
    {
        runContext?.ResumeGamePlay();

        ClosePopupIfOpen();
    }

    private void HandleLevelUpPopupFailure(RunContext runContext)
    {
        Debug.LogError("[LevelUpRewardFlow] LevelUp popup could not be shown.");
        Reset();
        runContext?.ResumeGamePlay();
    }

    private void ClosePopupIfOpen()
    {
        UIManager uiManager = UIManager.Instance;
        if (uiManager == null)
            return;

        if (activeLevelUpPopup == null)
            return;

        uiManager.ClosePopup(activeLevelUpPopup);
        activeLevelUpPopup = null;
    }
}
