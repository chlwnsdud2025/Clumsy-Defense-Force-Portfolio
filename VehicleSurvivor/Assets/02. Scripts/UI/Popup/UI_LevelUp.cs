using System.Collections.Generic;
using UnityEngine;

public class UI_LevelUp : UIPopup
{
    [SerializeField] private UI_SelectCard[] rewardCards;

    private RunManager runManager;
    private List<RewardOption> rewards = new();
    private bool rewardApplied;

    private void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.Space))
        {
            SelectFirstReward();
        }
    }

    public override void OnClickBack()
    {
        SelectFirstReward();
    }

    public void OnClickContinue()
    {
        SelectFirstReward();
    }

    public void ShowRewards(RunManager runManager, List<RewardOption> rewards)
    {
        this.runManager = runManager;
        this.rewards = rewards ?? new List<RewardOption>();
        rewardApplied = false;

        EnsureRewardCards();
        BindRewardCards();
    }

    private void EnsureRewardCards()
    {
        if (rewardCards != null && rewardCards.Length > 0)
            return;

        rewardCards = GetComponentsInChildren<UI_SelectCard>(true);

        if (rewardCards != null && rewardCards.Length > 0)
            return;

        List<UI_SelectCard> cards = new();
        AddCardIfFound(cards, "Item_01");
        AddCardIfFound(cards, "Item_02");
        AddCardIfFound(cards, "Item_03");

        rewardCards = cards.ToArray();
    }

    private void AddCardIfFound(List<UI_SelectCard> cards, string objectName)
    {
        Transform target = FindChildByName(transform, objectName);
        if (target == null)
            return;

        UI_SelectCard card = target.GetComponent<UI_SelectCard>();
        if (card == null)
            card = target.gameObject.AddComponent<UI_SelectCard>();

        cards.Add(card);
    }

    private static Transform FindChildByName(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildByName(root.GetChild(i), objectName);
            if (result != null)
                return result;
        }

        return null;
    }

    private void BindRewardCards()
    {
        for (int i = 0; i < rewardCards.Length; i++)
        {
            RewardOption reward = i < rewards.Count ? rewards[i] : null;
            rewardCards[i].Configure(reward, SelectReward);
        }
    }

    private void SelectFirstReward()
    {
        if (rewards == null || rewards.Count == 0)
        {
            ResumeGameplayWithoutReward();
            return;
        }

        SelectReward(rewards[0]);
    }

    private void SelectReward(RewardOption reward)
    {
        if (rewardApplied)
            return;

        rewardApplied = true;

        RunManager targetRunManager = runManager != null ? runManager : RunManager.Instance;
        if (targetRunManager == null || !targetRunManager.ApplyReward(reward))
        {
            rewardApplied = false;
            return;
        }
    }

    private void ResumeGameplayWithoutReward()
    {
        RunContext runContext = RunManager.Instance?.RunContext;
        if (runContext != null)
            runContext.ResumeGamePlay();
        else
            UIManager.Instance?.ClosePopup(this);
    }
}
