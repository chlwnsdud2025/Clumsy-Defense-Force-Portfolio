using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class UI_Pause : UIPopup
{
    [SerializeField] private SettingPanel settingPanel;
    [SerializeField] private PlayerStatsPanel playerStatsPanel;

    [Header("진입 애니메이션")]
    [SerializeField] private float enterDuration = 0.35f;
    [SerializeField] private Ease enterEase = Ease.OutBack;

    public override async UniTask InitializeAsync()
    {
        if (settingPanel == null)
            settingPanel = GetComponentInChildren<SettingPanel>(true);

        if (playerStatsPanel == null)
            playerStatsPanel = GetComponentInChildren<PlayerStatsPanel>(true);

        ConfigurePanels();
        await UniTask.Yield();
    }

    public override void OnShow()
    {
        base.OnShow();
        PauseRun();
        settingPanel?.Refresh();
        RefreshPlayerStats();
        PlayEnterAnimations();
    }

    public override void OnHide()
    {
        settingPanel?.StopEnterAnimation();
        playerStatsPanel?.StopEnterAnimation();
        playerStatsPanel?.Unbind();
        ResumeRun();
        base.OnHide();
    }

    public override void OnClickBack()
    {
        base.OnClickBack();
    }

    private void OnDestroy()
    {
        settingPanel?.StopEnterAnimation();
        playerStatsPanel?.StopEnterAnimation();
    }

    private void ConfigurePanels()
    {
        settingPanel?.Configure(enterDuration, enterEase);
        playerStatsPanel?.Configure(enterDuration, enterEase);
    }

    private void PlayEnterAnimations()
    {
        settingPanel?.PlayEnterAnimation();
        playerStatsPanel?.PlayEnterAnimation();
    }

    private void PauseRun()
    {
        if (!RunManager.TryGetInstance(out RunManager runManager))
            return;

        runManager.RunSession?.Pause();
    }

    private void ResumeRun()
    {
        if (!RunManager.TryGetInstance(out RunManager runManager))
            return;

        runManager.RunSession?.ResumeGamePlay();
    }

    private void RefreshPlayerStats()
    {
        if (playerStatsPanel == null)
            return;

        if (!RunManager.TryGetInstance(out RunManager runManager))
        {
            playerStatsPanel.Unbind();
            return;
        }

        playerStatsPanel.Bind(runManager.RunSession?.Player);
    }
}
