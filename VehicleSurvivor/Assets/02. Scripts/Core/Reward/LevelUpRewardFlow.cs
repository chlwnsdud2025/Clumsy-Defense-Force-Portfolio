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

    public void OnLevelChanged(RunSession runSession, int levelsGained, RewardManager rewardManager)
    {
        if (runSession == null || !runSession.ShouldUpdateGameplay)
            return;

        if (levelsGained <= 0)
            return;

        pendingLevelUpRewardCount += levelsGained;
        BeginLevelUpRewardsAsync(runSession, rewardManager).Forget();
    }

    public void OnRewardApplied(RewardManager rewardManager, RunSession runSession)
    {
        pendingLevelUpRewardCount--;

        if (pendingLevelUpRewardCount > 0)
        {
            PresentLevelUpPopup(rewardManager, runSession);
            return;
        }

        FinishLevelUpRewards(runSession);
    }

    public void OnRunFinished()
    {
        ClosePopupIfOpen();
        Reset();
    }

    private async UniTaskVoid BeginLevelUpRewardsAsync(RunSession runSession, RewardManager rewardManager)
    {
        if (runSession == null || pendingLevelUpRewardCount <= 0)
            return;

        if (activeLevelUpPopup != null)
        {
            PresentLevelUpPopup(rewardManager, runSession);
            return;
        }

        if (runSession.State != ERunState.Playing)
            return;

        runSession.EnterRewardSelection();

        UIManager uiManager = UIManager.Instance;
        if (uiManager == null)
        {
            HandleLevelUpPopupFailure(runSession);
            return;
        }

        activeLevelUpPopup = await uiManager.ShowPopupAsync<UI_LevelUp>(EPopupUIType.LevelUp, remember: false);
        if (rewardManager.RunSession == null || runSession.State != ERunState.RewardSelection)
            return;

        if (activeLevelUpPopup == null)
        {
            HandleLevelUpPopupFailure(runSession);
            return;
        }

        PresentLevelUpPopup(rewardManager, runSession);
    }

    private void PresentLevelUpPopup(RewardManager rewardManager, RunSession runSession)
    {
        if (activeLevelUpPopup == null || runSession == null || rewardManager == null)
            return;

        List<RewardOption> rewardOptions = rewardManager.GenerateLevelUpRewardOptions();
        if (rewardOptions.Count == 0)
            Debug.LogWarning("[LevelUpRewardFlow] 레벨업 보상 후보 목록이 비어 있습니다.");

        activeLevelUpPopup.ShowRewards(
            rewardManager,
            rewardOptions,
            runSession.Player.Level.Level);
    }

    private void FinishLevelUpRewards(RunSession runSession)
    {
        runSession?.ResumeGamePlay();

        ClosePopupIfOpen();
    }

    private void HandleLevelUpPopupFailure(RunSession runSession)
    {
        Debug.LogError("[LevelUpRewardFlow] LevelUp 팝업을 표시할 수 없습니다.");
        Reset();
        runSession?.ResumeGamePlay();
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
