using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

public class UI_SelectCard : UIPopup
{
    [Header("보상 선택 카드")]
    [Tooltip("비워 두면 자식 SelectCardFrame을 자동으로 찾습니다.")]
    [SerializeField] private List<SelectCardFrame> frames = new();

    [Header("등급 비주얼")]
    [Tooltip("카드 등급 라벨과 Front Bg 색상")]
    [SerializeField] private ItemRarityVisualsSO rarityVisuals;

    private readonly List<RewardOption> rewards = new();
    private Action<RewardOption> onRewardSelected;

    private int selectedIndex = -1;
    private bool hasConfirmed;

    protected override bool CanCloseOnBackdropClick => false;

    public override async UniTask InitializeAsync()
    {
        if (frames.Count == 0)
        {
            SelectCardFrame[] discoveredFrames = GetComponentsInChildren<SelectCardFrame>(true);
            frames.AddRange(discoveredFrames);
        }

        RegisterFrameEvents();
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

    public void ShowRewards(List<RewardOption> rewardOptions, Action<RewardOption> onSelected)
    {
        BindRewards(rewardOptions, onSelected);
    }

    private void BindRewards(List<RewardOption> rewardOptions, Action<RewardOption> onSelected)
    {
        rewards.Clear();
        if (rewardOptions != null)
            rewards.AddRange(rewardOptions);

        onRewardSelected = onSelected;

        for (int i = 0; i < frames.Count; i++)
        {
            if (frames[i] == null)
                continue;

            if (i < rewards.Count)
                frames[i].Bind(rewards[i], rarityVisuals);
            else
                frames[i].Clear();
        }

        ClearSelection();
        SetSelectedIndex(FindFirstNavigableIndex());
    }

    private void OnDestroy()
    {
        UnregisterFrameEvents();
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy || hasConfirmed)
            return;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            MoveSelection(-1);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            MoveSelection(1);
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            ConfirmSelection();
    }

    private void RegisterFrameEvents()
    {
        for (int i = 0; i < frames.Count; i++)
        {
            if (frames[i] == null)
                continue;

            frames[i].Hovered -= HandleFrameHovered;
            frames[i].Hovered += HandleFrameHovered;
            frames[i].Clicked -= HandleFrameClicked;
            frames[i].Clicked += HandleFrameClicked;
        }
    }

    private void UnregisterFrameEvents()
    {
        for (int i = 0; i < frames.Count; i++)
        {
            if (frames[i] == null)
                continue;

            frames[i].Hovered -= HandleFrameHovered;
            frames[i].Clicked -= HandleFrameClicked;
        }
    }

    private void HandleFrameHovered(SelectCardFrame frame)
    {
        if (hasConfirmed)
            return;

        int index = frames.IndexOf(frame);
        if (index < 0)
            return;

        SetSelectedIndex(index);
    }

    private void HandleFrameClicked(SelectCardFrame frame)
    {
        if (hasConfirmed)
            return;

        int index = frames.IndexOf(frame);
        if (index < 0 || !frames[index].HasData)
            return;

        hasConfirmed = true;
        SetSelectedIndex(index);
        ApplyRewardAtIndex(index);
    }

    private void MoveSelection(int direction)
    {
        if (frames.Count == 0)
            return;

        int startIndex = selectedIndex >= 0 ? selectedIndex : 0;
        int index = startIndex;

        for (int step = 0; step < frames.Count; step++)
        {
            index = WrapIndex(index + direction, frames.Count);
            if (IsFrameNavigable(frames[index]))
            {
                SetSelectedIndex(index);
                return;
            }
        }
    }

    private void SetSelectedIndex(int index)
    {
        if (index < 0 || index >= frames.Count)
            return;

        if (!IsFrameNavigable(frames[index]))
            return;

        if (selectedIndex >= 0 && selectedIndex < frames.Count)
            frames[selectedIndex].SetSelected(false);

        selectedIndex = index;
        frames[selectedIndex].SetSelected(true);
    }

    private void ConfirmSelection()
    {
        if (hasConfirmed)
            return;

        if (selectedIndex < 0 || selectedIndex >= frames.Count)
            return;

        if (!frames[selectedIndex].HasData)
            return;

        hasConfirmed = true;
        ApplyRewardAtIndex(selectedIndex);
    }

    private void ApplyRewardAtIndex(int index)
    {
        if (index < 0 || index >= frames.Count)
            return;

        SelectCardFrame selectedFrame = frames[index];
        if (!selectedFrame.HasData)
            return;

        RewardOption selectedReward = selectedFrame.Reward;
        Debug.Log($"[UI_SelectCard] 보상 선택: index={index}, 이름={selectedReward.DisplayName}");

        onRewardSelected?.Invoke(selectedReward);
    }

    private void ClearSelection()
    {
        if (selectedIndex >= 0 && selectedIndex < frames.Count)
            frames[selectedIndex].SetSelected(false);

        selectedIndex = -1;
        hasConfirmed = false;
    }

    private int FindFirstNavigableIndex()
    {
        for (int i = 0; i < frames.Count; i++)
        {
            if (IsFrameNavigable(frames[i]))
                return i;
        }

        return -1;
    }

    private static bool IsFrameNavigable(SelectCardFrame frame)
    {
        return frame != null && frame.gameObject.activeInHierarchy && frame.HasData;
    }

    private static int WrapIndex(int index, int count)
    {
        if (count <= 0)
            return -1;

        if (index < 0)
            return count - 1;

        if (index >= count)
            return 0;

        return index;
    }
}
