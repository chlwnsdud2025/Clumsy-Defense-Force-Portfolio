using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_LevelUp : UIPopup
{
    [Header("보상 선택 패널")]
    [SerializeField] private List<RewardChoicePanel> panels = new();

    [Header("Popup Content")]
    [SerializeField] private RectTransform popupContent;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private PlayerStatsPanel playerStatsPanel;

    [Header("열기 애니메이션")]
    [SerializeField] private float openScaleDuration = 0.35f;
    [SerializeField] private Ease openScaleEase = Ease.OutBack;

    private List<RewardOption> rewards = new();
    private System.Action<RewardOption> onRewardSelected;

    private Tween openScaleTween;
    private bool hasPlayedPlayerStatsEnterAnimation;

    private int selectedIndex = -1;
    private bool hasConfirmed;

    public override async UniTask InitializeAsync()
    {
        if (playerStatsPanel == null)
            playerStatsPanel = GetComponentInChildren<PlayerStatsPanel>(true);

        ConfigurePlayerStatsPanel();
        RegisterPanelEvents();

        await UniTask.Yield();
    }

    public override void OnShow()
    {
        base.OnShow();
        ClearSelection();
        SetSelectedIndex(FindFirstNavigableIndex());
        RefreshPlayerStats();
    }

    public override void OnHide()
    {
        playerStatsPanel?.StopEnterAnimation();
        playerStatsPanel?.Unbind();
        KillOpenTween();
        ClearSelection();
        hasPlayedPlayerStatsEnterAnimation = false;
        base.OnHide();
    }

    public void ShowRewards(
        RewardManager rewardManager,
        List<RewardOption> rewardOptions,
        int currentLevel)
    {
        BindRewards(
            rewardOptions,
            reward => rewardManager?.ApplyReward(reward));

        RefreshPlayerStats();
        PlayOpenAnimation();
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

    private void PlayOpenAnimation()
    {
        KillOpenTween();

        if (popupContent != null)
        {
            popupContent.localScale = Vector3.zero;
            openScaleTween = popupContent
                .DOScale(Vector3.one, openScaleDuration)
                .SetEase(openScaleEase)
                .SetUpdate(true);
        }

        if (hasPlayedPlayerStatsEnterAnimation)
            return;

        playerStatsPanel?.PlayEnterAnimation();
        hasPlayedPlayerStatsEnterAnimation = true;
    }

    private void KillOpenTween()
    {
        if (openScaleTween != null && openScaleTween.IsActive())
            openScaleTween.Kill();

        openScaleTween = null;
    }

    private void BindRewards(List<RewardOption> rewardOptions, System.Action<RewardOption> onSelected)
    {
        rewards = rewardOptions ?? new List<RewardOption>();
        onRewardSelected = onSelected;

        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] == null)
                continue;

            if (i < rewards.Count)
                panels[i].Bind(rewards[i]);
            else
                panels[i].Clear();
        }

        ClearSelection();
        SetSelectedIndex(FindFirstNavigableIndex());
    }

    private void OnDestroy()
    {
        playerStatsPanel?.StopEnterAnimation();
        KillOpenTween();
        UnregisterPanelEvents();
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy || hasConfirmed)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            MoveSelection(-1);
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            MoveSelection(1);
        else if (Input.GetKeyDown(KeyCode.Return) ||
                 Input.GetKeyDown(KeyCode.KeypadEnter))
            ConfirmSelection();
    }

    private void RegisterPanelEvents()
    {
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] == null)
                continue;

            panels[i].Hovered -= HandlePanelHovered;
            panels[i].Hovered += HandlePanelHovered;
            panels[i].Clicked -= HandlePanelClicked;
            panels[i].Clicked += HandlePanelClicked;
        }
    }

    private void ConfigurePlayerStatsPanel()
    {
        if (playerStatsPanel == null)
            return;

        playerStatsPanel.Configure(openScaleDuration, openScaleEase);
    }

    private void HandlePanelHovered(RewardChoicePanel panel)
    {
        if (hasConfirmed)
            return;

        int index = panels.IndexOf(panel);
        if (index < 0)
            return;

        SetSelectedIndex(index);
    }

    private void HandlePanelClicked(RewardChoicePanel panel)
    {
        if (hasConfirmed)
            return;

        int index = panels.IndexOf(panel);
        if (index < 0 || !panels[index].HasData)
            return;

        hasConfirmed = true;
        SoundPlayback.PlaySfx(ESoundId.SFX_Reward_Select);
        SetSelectedIndex(index);
        ConfirmAfterFocusAsync(panel, index).Forget();
    }

    private async UniTaskVoid ConfirmAfterFocusAsync(RewardChoicePanel panel, int index)
    {
        float delay = panel.FocusTweenDuration;
        await UniTask.Delay(TimeSpan.FromSeconds(delay), ignoreTimeScale: true);
        ApplyRewardAtIndex(index);
    }

    private void MoveSelection(int direction)
    {
        if (panels.Count == 0)
            return;

        int startIndex = selectedIndex >= 0 ? selectedIndex : 0;
        int index = startIndex;

        for (int step = 0; step < panels.Count; step++)
        {
            index = WrapIndex(index + direction, panels.Count);
            if (IsPanelNavigable(panels[index]))
            {
                SetSelectedIndex(index);
                return;
            }
        }
    }

    private void SetSelectedIndex(int index)
    {
        if (index < 0 || index >= panels.Count)
            return;

        if (!IsPanelNavigable(panels[index]))
            return;

        if (selectedIndex >= 0 && selectedIndex < panels.Count)
            panels[selectedIndex].SetSelected(false);

        selectedIndex = index;
        panels[selectedIndex].SetSelected(true);
    }

    private void ConfirmSelection()
    {
        if (hasConfirmed)
            return;

        if (selectedIndex < 0 || selectedIndex >= panels.Count)
            return;

        if (!panels[selectedIndex].HasData)
            return;

        hasConfirmed = true;
        SoundPlayback.PlaySfx(ESoundId.SFX_Reward_Select);
        ApplyRewardAtIndex(selectedIndex);
    }

    private void ApplyRewardAtIndex(int index)
    {
        if (index < 0 || index >= panels.Count)
            return;

        RewardChoicePanel selectedPanel = panels[index];
        if (!selectedPanel.HasData)
            return;

        RewardOption selectedReward = selectedPanel.Reward;
        Debug.Log($"[UI_LevelUp] 보상 선택: index={index}, name={selectedReward.DisplayName}");

        SoundPlayback.PlaySfx(ESoundId.SFX_Reward_Apply);
        onRewardSelected?.Invoke(selectedReward);
    }

    private void ClearSelection()
    {
        if (selectedIndex >= 0 && selectedIndex < panels.Count)
            panels[selectedIndex].SetSelected(false);

        selectedIndex = -1;
        hasConfirmed = false;
    }

    private int FindFirstNavigableIndex()
    {
        for (int i = 0; i < panels.Count; i++)
        {
            if (IsPanelNavigable(panels[i]))
                return i;
        }

        return -1;
    }

    private bool IsPanelNavigable(RewardChoicePanel panel)
    {
        return panel != null && panel.gameObject.activeInHierarchy;
    }

    private int WrapIndex(int index, int count)
    {
        if (count <= 0)
            return -1;

        if (index < 0)
            return count - 1;

        if (index >= count)
            return 0;

        return index;
    }

    private void UnregisterPanelEvents()
    {
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] == null)
                continue;

            panels[i].Hovered -= HandlePanelHovered;
            panels[i].Clicked -= HandlePanelClicked;
        }
    }

    public void OnClickContinue()
    {
        ConfirmSelection();
    }
}
