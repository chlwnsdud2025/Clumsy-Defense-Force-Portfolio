using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public readonly struct RewardChoiceFocusSettings
{
    public RewardChoiceFocusSettings(
        float focusedScale,
        float tweenDuration,
        Ease tweenEase)
    {
        FocusedScale = focusedScale;
        TweenDuration = tweenDuration;
        TweenEase = tweenEase;
    }

    public float FocusedScale { get; }
    public float TweenDuration { get; }
    public Ease TweenEase { get; }
}

public class RewardChoicePanel : MonoBehaviour
{
    private const string NewLevelLabel = "NEW!";

    [Header("레벨 패널 색상")]
    [SerializeField] private Color newItemLevelTextPanelColor;

    [Header("표시 UI")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image levelTextPanelImage;

    [Header("상호작용")]
    [SerializeField] private GameObject focusObject;
    [SerializeField] private Button button;
    [SerializeField] private RectTransform scaleTarget;

    public event Action<RewardChoicePanel> Clicked;

    public bool IsSelectable => gameObject.activeInHierarchy;
    public bool HasData { get; private set; }

    private float focusedScale = 1.05f;
    private float focusTweenDuration = 0.15f;
    private Ease focusTweenEase = Ease.OutQuad;
    private Color normalItemLevelTextPanelColor;
    private Vector3 normalScale;
    private Tween focusScaleTween;

    private void Awake()
    {
        if (scaleTarget == null)
            scaleTarget = transform as RectTransform;

        normalScale = scaleTarget.localScale;

        if (levelTextPanelImage != null)
            normalItemLevelTextPanelColor = levelTextPanelImage.color;

        RegisterButtonClick();
        SetSelected(false);
    }

    private void OnDisable()
    {
        KillFocusTweens();
        scaleTarget.localScale = normalScale;
    }

    private void OnDestroy()
    {
        KillFocusTweens();
        UnregisterButtonClick();
    }

    public void Configure(RewardChoiceFocusSettings settings)
    {
        focusedScale = settings.FocusedScale;
        focusTweenDuration = settings.TweenDuration;
        focusTweenEase = settings.TweenEase;
    }

    public void Bind(PassiveDefinitionSO passive, bool isNew, int level = 1)
    {
        // TODO: PassiveDefinitionSO 데이터를 itemIcon, nameText, descriptionText, levelText에 적용
        HasData = passive != null;

        if (!HasData)
        {
            Clear();
            return;
        }

        SetSelected(false);
        gameObject.SetActive(true);
    }

    public void Bind(WeaponDefinitionSO weapon, bool isNew, int level = 1)
    {
        // TODO: WeaponDefinitionSO 데이터를 itemIcon, nameText, descriptionText, levelText에 적용
        HasData = weapon != null;

        if (!HasData)
        {
            Clear();
            return;
        }

        SetSelected(false);
        gameObject.SetActive(true);
    }

    public void Clear()
    {
        HasData = false;

        itemIcon.sprite = null;
        itemIcon.enabled = false;
        nameText.text = string.Empty;
        descriptionText.text = string.Empty;
        levelText.text = string.Empty;
        levelTextPanelImage.color = normalItemLevelTextPanelColor;

        SetSelected(false);
        gameObject.SetActive(false);
    }

    public void SetSelected(bool isSelected)
    {
        bool showFocus = isSelected && IsSelectable;
        focusObject.SetActive(showFocus);
        PlayFocusScaleTween(showFocus);
    }

    private void PlayFocusScaleTween(bool isFocused)
    {
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
        levelText.text = isNew ? NewLevelLabel : level.ToString();
        levelTextPanelImage.color = isNew
            ? newItemLevelTextPanelColor
            : normalItemLevelTextPanelColor;
    }

    private void RegisterButtonClick()
    {
        button.onClick.RemoveListener(HandleButtonClicked);
        button.onClick.AddListener(HandleButtonClicked);
    }

    private void UnregisterButtonClick()
    {
        button.onClick.RemoveListener(HandleButtonClicked);
    }

    private void HandleButtonClicked()
    {
        if (!IsSelectable)
            return;

        Clicked?.Invoke(this);
    }
}
