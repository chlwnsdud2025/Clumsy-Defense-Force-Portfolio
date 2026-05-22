using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardChoicePanel : MonoBehaviour
{
    private const string NewLevelLabel = "NEW!";

    [Header("레벨 패널 색상")]
    [SerializeField] private Color newItemLevelTextPanelColor;

    [Header("Display UI")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image levelTextPanelImage;

    [Header("상호작용")]
    [SerializeField] private GameObject focusObject;
    [SerializeField] private Button button;
    [SerializeField] private RectTransform scaleTarget;

    [Header("Focus Effect")]
    [SerializeField] private float focusedScale = 1.05f;
    [SerializeField] private float focusTweenDuration = 0.15f;
    [SerializeField] private Ease focusTweenEase = Ease.OutQuad;

    public event Action<RewardChoicePanel> Clicked;

    public RewardOption Reward { get; private set; }
    public bool IsSelectable => gameObject.activeInHierarchy;
    public bool HasData { get; private set; }
    private Color normalItemLevelTextPanelColor;
    private Vector3 normalScale;
    private Tween focusScaleTween;

    private void Awake()
    {
        if (scaleTarget == null)
            scaleTarget = transform as RectTransform;

        normalScale = scaleTarget != null ? scaleTarget.localScale : Vector3.one;

        if (levelTextPanelImage != null)
            normalItemLevelTextPanelColor = levelTextPanelImage.color;

        RegisterButtonClick();
        SetSelected(false);
    }

    private void OnDisable()
    {
        KillFocusTweens();

        if (scaleTarget != null)
            scaleTarget.localScale = normalScale;
    }

    private void OnDestroy()
    {
        KillFocusTweens();
        UnregisterButtonClick();
    }

    public void Bind(PassiveDefinitionSO passive, bool isNew, int level = 1)
    {
        Bind(passive != null ? RewardOption.PassiveReward(passive, isNew, level) : null);
    }

    public void Bind(WeaponDefinitionSO weapon, bool isNew, int level = 1)
    {
        Bind(weapon != null ? RewardOption.WeaponReward(weapon, isNew, level) : null);
    }

    public void Bind(RewardOption reward)
    {
        Reward = reward;
        HasData = reward != null;

        if (!HasData)
        {
            Clear();
            return;
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = reward.Icon;
            itemIcon.enabled = reward.Icon != null;
        }

        if (nameText != null)
            nameText.text = reward.DisplayName;

        if (descriptionText != null)
            descriptionText.text = reward.Description;

        ApplyLevelDisplay(reward.IsNew, reward.Level);
        SetSelected(false);
        gameObject.SetActive(true);
    }

    public void Clear()
    {
        Reward = null;
        HasData = false;

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if (nameText != null)
            nameText.text = string.Empty;

        if (descriptionText != null)
            descriptionText.text = string.Empty;

        if (levelText != null)
            levelText.text = string.Empty;

        if (levelTextPanelImage != null)
            levelTextPanelImage.color = normalItemLevelTextPanelColor;

        SetSelected(false);
        gameObject.SetActive(false);
    }

    public void SetSelected(bool isSelected)
    {
        bool showFocus = isSelected && IsSelectable;

        if (focusObject != null)
            focusObject.SetActive(showFocus);

        PlayFocusScaleTween(showFocus);
    }

    private void PlayFocusScaleTween(bool isFocused)
    {
        if (scaleTarget == null)
            return;

        focusScaleTween?.Kill();

        Vector3 targetScale = isFocused
            ? normalScale * focusedScale
            : normalScale;

        focusScaleTween = scaleTarget
            .DOScale(targetScale, focusTweenDuration)
            .SetEase(focusTweenEase);
    }

    private void KillFocusTweens()
    {
        focusScaleTween?.Kill();
        focusScaleTween = null;
    }

    private void ApplyLevelDisplay(bool isNew, int level)
    {
        if (levelText != null)
            levelText.text = isNew ? NewLevelLabel : level.ToString();

        if (levelTextPanelImage != null)
        {
            levelTextPanelImage.color = isNew
                ? newItemLevelTextPanelColor
                : normalItemLevelTextPanelColor;
        }
    }

    private void RegisterButtonClick()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(HandleButtonClicked);
        button.onClick.AddListener(HandleButtonClicked);
    }

    private void UnregisterButtonClick()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleButtonClicked);
    }

    private void HandleButtonClicked()
    {
        if (!IsSelectable)
            return;

        Clicked?.Invoke(this);
    }
}
