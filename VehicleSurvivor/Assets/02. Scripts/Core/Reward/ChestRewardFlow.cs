using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class ChestRewardFlow
{
    private UI_SelectCard activePopup;
    private bool isPresenting;

    public void Reset()
    {
        activePopup = null;
        isPresenting = false;
    }

    public void OnRunFinished()
    {
        ClosePopupIfOpen();
        Reset();
    }

    public void BeginChestReward(RunSession runSession, RewardManager rewardManager)
    {
        BeginChestRewardAsync(runSession, rewardManager).Forget();
    }

    public void OnRewardApplied(RunSession runSession)
    {
        runSession?.ResumeGamePlay();
        ClosePopupIfOpen();
        isPresenting = false;
    }

    private async UniTaskVoid BeginChestRewardAsync(RunSession runSession, RewardManager rewardManager)
    {
        if (runSession == null || rewardManager == null)
            return;

        if (isPresenting || activePopup != null)
            return;

        if (runSession.State != ERunState.Playing)
            return;

        isPresenting = true;
        runSession.EnterRewardSelection();

        UIManager uiManager = UIManager.Instance;
        if (uiManager == null)
        {
            HandleFailure(runSession);
            return;
        }

        activePopup = await uiManager.ShowPopupAsync<UI_SelectCard>(EPopupUIType.SelectCard, remember: false);
        if (rewardManager.RunSession == null || runSession.State != ERunState.RewardSelection)
        {
            isPresenting = false;
            return;
        }

        if (activePopup == null)
        {
            HandleFailure(runSession);
            return;
        }

        PresentChestPopup(rewardManager);
    }

    private void PresentChestPopup(RewardManager rewardManager)
    {
        if (activePopup == null || rewardManager == null)
            return;

        List<RewardOption> rewardOptions = rewardManager.GenerateChestRewardOptions();
        if (rewardOptions.Count == 0)
            Debug.LogWarning("[ChestRewardFlow] 상자 보상 후보 목록이 비어 있습니다.");

        activePopup.ShowRewards(rewardOptions, reward => rewardManager.ApplyChestReward(reward));
    }

    private void HandleFailure(RunSession runSession)
    {
        Debug.LogError("[ChestRewardFlow] SelectCard 팝업을 표시할 수 없습니다.");
        isPresenting = false;
        runSession?.ResumeGamePlay();
    }

    private void ClosePopupIfOpen()
    {
        UIManager uiManager = UIManager.Instance;
        if (uiManager == null)
            return;

        if (activePopup == null)
            return;

        uiManager.ClosePopup(activePopup);
        activePopup = null;
    }
}
