using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SelectCard : UIPopup
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    private RewardOption reward;
    private Action<RewardOption> onSelected;

    private void Awake()
    {
        ResolveReferences();
        RegisterButton();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClicked);
    }

    public void Configure(RewardOption reward, Action<RewardOption> onSelected)
    {
        ResolveReferences();
        RegisterButton();

        this.reward = reward;
        this.onSelected = onSelected;

        bool hasReward = reward != null;
        gameObject.SetActive(hasReward);

        if (!hasReward)
            return;

        if (titleText != null)
            titleText.text = reward.DisplayName;

        if (descriptionText != null)
            descriptionText.text = reward.Description;

        if (iconImage != null)
        {
            iconImage.sprite = reward.Icon;
            iconImage.enabled = reward.Icon != null;
        }
    }

    public void Clear()
    {
        Configure(null, null);
    }

    private void ResolveReferences()
    {
        if (button == null)
            button = GetComponent<Button>();

        Image surface = GetComponent<Image>();
        if (surface == null)
        {
            surface = gameObject.AddComponent<Image>();
            surface.color = new Color(1f, 1f, 1f, 0f);
        }

        surface.raycastTarget = true;

        if (button == null)
            button = gameObject.AddComponent<Button>();

        if (button.targetGraphic == null)
            button.targetGraphic = surface;

        if (iconImage == null)
            iconImage = FindChildComponentByName<Image>("Icon") ?? FindChildComponentByName<Image>("ItemIcon");

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        if (titleText == null && texts.Length > 0)
            titleText = texts[0];

        if (descriptionText == null && texts.Length > 1)
            descriptionText = texts[1];
    }

    private void RegisterButton()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(HandleClicked);
        button.onClick.AddListener(HandleClicked);
    }

    private T FindChildComponentByName<T>(string childName) where T : Component
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == childName && child.TryGetComponent(out T component))
                return component;
        }

        return null;
    }

    private void HandleClicked()
    {
        if (reward == null)
            return;

        onSelected?.Invoke(reward);
    }
}
