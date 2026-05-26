using Cysharp.Threading.Tasks;
using DG.Tweening;
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

    [Header("등장 연출")]
    [Header("1단계 - Scale 등장")]
    [SerializeField] private float introScaleDuration = 0.4f;
    [SerializeField] private Ease introScaleEase = Ease.OutBack;
    [Header("2단계 - 뒤집기 준비")]
    [SerializeField] private float flipDelayAfterScale = 0.15f;
    [Header("3단계 - 기준 등급 이하 그룹 뒤집기")]
    [Tooltip("기준 등급 이하 그룹과 이상 그룹 사이 대기 시간")]
    [SerializeField] private float flipGroupInterval = 0.2f;
    [Tooltip("이 등급 이하는 한 번에, 초과 등급은 멈칫+흔들림 후 한 번에 뒤집습니다.")]
    [SerializeField] private EItemRarity flipGroupThreshold = EItemRarity.Uncommon;
    [Header("4단계 - 기준 등급 초과 그룹 (멈칫 → 흔들림 → 대기 → 뒤집기)")]
    [Tooltip("기준 등급 초과 그룹 뒤집기 전 멈칫하는 시간")]
    [SerializeField] private float premiumFlipPauseDuration = 0.25f;
    [SerializeField] private float premiumFlipShakeDuration = 0.35f;
    [SerializeField] private float premiumFlipShakeStrength = 12f;
    [Tooltip("기준 등급 초과 그룹 흔들린 후 뒤집기 전 대기 시간")]
    [SerializeField] private float premiumFlipDelayAfterShake = 0.15f;

    private readonly List<RewardOption> rewards = new();
    private Action<RewardOption> onRewardSelected;

    private int selectedIndex = -1;
    private bool hasConfirmed;
    private bool isIntroPlaying;

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
    }

    public override void OnHide()
    {
        KillIntroTweens();
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
        PlayIntroAsync().Forget();
    }

    private void OnDestroy()
    {
        KillIntroTweens();
        UnregisterFrameEvents();
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy || hasConfirmed || isIntroPlaying)
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
        if (hasConfirmed || isIntroPlaying)
            return;

        int index = frames.IndexOf(frame);
        if (index < 0)
            return;

        SetSelectedIndex(index);
    }

    private void HandleFrameClicked(SelectCardFrame frame)
    {
        if (hasConfirmed || isIntroPlaying)
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
        if (hasConfirmed || isIntroPlaying)
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

    private async UniTaskVoid PlayIntroAsync()
    {
        KillIntroTweens();
        isIntroPlaying = true;

        List<SelectCardFrame> activeFrames = CollectActiveFrames();
        if (activeFrames.Count == 0)
        {
            isIntroPlaying = false;
            return;
        }

        // 0단계: 뒷면 + Scale 0 상태로 초기화
        for (int i = 0; i < activeFrames.Count; i++)
            activeFrames[i].PrepareIntroBackFace();

        // 1단계: 모든 카드 Scale 0 → 1 (동시)
        List<UniTask> scaleTasks = new(activeFrames.Count);
        for (int i = 0; i < activeFrames.Count; i++)
            scaleTasks.Add(activeFrames[i].PlayIntroScaleAsync(introScaleDuration, introScaleEase));

        await UniTask.WhenAll(scaleTasks);

        // 2단계: Scale 등장 후 잠시 대기
        if (flipDelayAfterScale > 0f)
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(flipDelayAfterScale),
                ignoreTimeScale: true,
                cancellationToken: destroyCancellationToken);
        }

        SplitFlipGroups(activeFrames, out List<SelectCardFrame> standardGroup, out List<SelectCardFrame> premiumGroup);

        // 3단계: 기준 등급 이하 그룹 동시 뒤집기
        if (standardGroup.Count > 0)
            await FlipFramesTogetherAsync(standardGroup);

        if (premiumGroup.Count > 0)
        {
            // 3→4단계 사이: 아직 안 뒤집힌 고등급 카드로 기대감 유지
            if (standardGroup.Count > 0 && flipGroupInterval > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(flipGroupInterval),
                    ignoreTimeScale: true,
                    cancellationToken: destroyCancellationToken);
            }

            // 4-1단계: 멈칫 + 흔들림 (고등급 그룹 동시)
            List<UniTask> shakeTasks = new(premiumGroup.Count);
            for (int i = 0; i < premiumGroup.Count; i++)
            {
                shakeTasks.Add(premiumGroup[i].PlayAnticipationShakeAsync(
                    premiumFlipPauseDuration,
                    premiumFlipShakeDuration,
                    premiumFlipShakeStrength));
            }

            await UniTask.WhenAll(shakeTasks);

            // 4-2단계: 흔들린 후 뒤집기 전 대기
            if (premiumFlipDelayAfterShake > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(premiumFlipDelayAfterShake),
                    ignoreTimeScale: true,
                    cancellationToken: destroyCancellationToken);
            }

            // 4-3단계: 기준 등급 초과 그룹 동시 뒤집기
            await FlipFramesTogetherAsync(premiumGroup);
        }

        // 5단계: 연출 종료, 입력 활성화 + 첫 카드 선택
        for (int i = 0; i < activeFrames.Count; i++)
            activeFrames[i].SetInteractionEnabled(true);

        isIntroPlaying = false;
        SetSelectedIndex(FindFirstNavigableIndex());
    }

    private List<SelectCardFrame> CollectActiveFrames()
    {
        List<SelectCardFrame> activeFrames = new();
        for (int i = 0; i < frames.Count; i++)
        {
            if (IsFrameNavigable(frames[i]))
                activeFrames.Add(frames[i]);
        }

        return activeFrames;
    }

    private async UniTask FlipFramesTogetherAsync(List<SelectCardFrame> framesToFlip)
    {
        List<UniTask> flipTasks = new(framesToFlip.Count);
        for (int i = 0; i < framesToFlip.Count; i++)
            flipTasks.Add(framesToFlip[i].PlayFlipRevealAsync(enableInteractionOnComplete: false));

        await UniTask.WhenAll(flipTasks);
    }

    /// <summary>flipGroupThreshold 기준으로 이하/초과 그룹 분리</summary>
    private void SplitFlipGroups(
        List<SelectCardFrame> activeFrames,
        out List<SelectCardFrame> standardGroup,
        out List<SelectCardFrame> premiumGroup)
    {
        standardGroup = new List<SelectCardFrame>();
        premiumGroup = new List<SelectCardFrame>();

        for (int i = 0; i < activeFrames.Count; i++)
        {
            if (IsPremiumFlipGroup(activeFrames[i].ItemRarity))
                premiumGroup.Add(activeFrames[i]);
            else
                standardGroup.Add(activeFrames[i]);
        }
    }

    private bool IsPremiumFlipGroup(EItemRarity rarity)
    {
        if (rarity == EItemRarity.None)
            return false;

        return rarity > flipGroupThreshold;
    }

    private void KillIntroTweens()
    {
        for (int i = 0; i < frames.Count; i++)
        {
            if (frames[i] == null)
                continue;

            if (frames[i].transform is RectTransform rect)
            {
                rect.DOKill();
                rect.localRotation = Quaternion.identity;
            }
        }
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
