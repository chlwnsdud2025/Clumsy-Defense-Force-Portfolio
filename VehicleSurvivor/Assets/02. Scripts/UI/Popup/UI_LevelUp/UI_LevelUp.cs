using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class UI_LevelUp : UIPopup
{
    [Header("Reward Choice Panels")]
    [SerializeField] private List<RewardChoicePanel> panels = new();

    [Header("Focus Effect")]
    [SerializeField] private float focusedScale = 1.05f;
    [SerializeField] private float focusTweenDuration = 0.15f;
    [SerializeField] private Ease focusTweenEase = Ease.OutQuad;

    private List<RewardOption> rewards = new();
    private System.Action<RewardOption> onRewardSelected;
    private int selectedIndex = -1;

    private void Awake()
    {
        ConfigurePanels();
    }

    public override async UniTask InitializeAsync()
    {
        ConfigurePanels();
        RegisterPanelEvents();

        await UniTask.Yield();
    }

    public override void OnShow()
    {
        base.OnShow();
        ClearSelection();
        SetSelectedIndex(FindFirstNavigableIndex());
    }

    public override void OnHide()
    {
        ClearSelection();
        base.OnHide();
    }

    public void ShowRewards(RunManager sourceRunManager, List<RewardOption> rewardOptions)
    {
        BindRewards(
            rewardOptions,
            reward =>
            {
                if (sourceRunManager == null || !sourceRunManager.ApplyReward(reward))
                    ResumeGameplay();
            });
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
        UnregisterPanelEvents();
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ResumeGameplay();
            return;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            MoveSelection(-1);
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            MoveSelection(1);
        else if (Input.GetKeyDown(KeyCode.Return) ||
                 Input.GetKeyDown(KeyCode.KeypadEnter) ||
                 Input.GetKeyDown(KeyCode.Space))
            ConfirmSelection();
    }

    private void RegisterPanelEvents()
    {
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] == null)
                continue;

            panels[i].Clicked -= HandlePanelClicked;
            panels[i].Clicked += HandlePanelClicked;
        }
    }

    private void ConfigurePanels()
    {
        RewardChoiceFocusSettings focusSettings = new RewardChoiceFocusSettings(
            focusedScale,
            focusTweenDuration,
            focusTweenEase);

        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] == null)
                continue;

            panels[i].Configure(focusSettings);
        }
    }

    private void HandlePanelClicked(RewardChoicePanel panel)
    {
        int index = panels.IndexOf(panel);
        if (index < 0)
            return;

        SetSelectedIndex(index);
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
        if (selectedIndex < 0 || selectedIndex >= panels.Count)
            return;

        RewardChoicePanel selectedPanel = panels[selectedIndex];
        if (!selectedPanel.HasData)
            return;

        RewardOption selectedReward = selectedPanel.Reward;
        Debug.Log($"[UI_LevelUp] Reward selected: index={selectedIndex}, name={selectedReward.DisplayName}");

        if (onRewardSelected != null)
            onRewardSelected.Invoke(selectedReward);
        else
            ResumeGameplay();
    }

    private void ClearSelection()
    {
        if (selectedIndex >= 0 && selectedIndex < panels.Count)
            panels[selectedIndex].SetSelected(false);

        selectedIndex = -1;
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
            if (panels[i] != null)
                panels[i].Clicked -= HandlePanelClicked;
        }
    }

    private void ResumeGameplay()
    {
        RunContext runContext = RunManager.Instance?.RunContext;
        if (runContext == null)
        {
            UIManager.Instance?.ClosePopup(this);
            return;
        }

        runContext.ResumeGamePlay();
    }

    public void OnClickContinue()
    {
        ConfirmSelection();
    }
}
