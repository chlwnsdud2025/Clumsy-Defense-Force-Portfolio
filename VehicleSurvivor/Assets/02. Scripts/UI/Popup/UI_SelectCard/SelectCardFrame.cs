using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectCardFrame : MonoBehaviour, IPointerEnterHandler
{
    [Header("표시 UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image frontBgImage;

    [Header("상호작용")]
    [SerializeField] private Button button;
    [SerializeField] private RectTransform scaleTarget;

    [Header("Selection Effect")]
    [SerializeField] private float selectedScale = 1.05f;
    [SerializeField] private float scaleTweenDuration = 0.15f;
    [SerializeField] private Ease scaleTweenEase = Ease.OutQuad;

    public float ScaleTweenDuration => scaleTweenDuration;

    public event Action<SelectCardFrame> Hovered;
    public event Action<SelectCardFrame> Clicked;

    public RewardOption Reward { get; private set; }
    public bool HasData { get; private set; }
    public bool IsSelectable => gameObject.activeInHierarchy && HasData;

    private Vector3 normalScale;
    private Tween scaleTween;

    private void Awake()
    {
        ResolveReferences();

        if (scaleTarget == null)
            scaleTarget = transform as RectTransform;

        normalScale = scaleTarget != null ? scaleTarget.localScale : Vector3.one;

        RegisterButtonClick();
        SetSelected(false);
    }

    private void OnDisable()
    {
        KillScaleTween();

        if (scaleTarget != null)
            scaleTarget.localScale = normalScale;
    }

    private void OnDestroy()
    {
        KillScaleTween();
        UnregisterButtonClick();
    }

    public void Bind(RewardOption reward, ItemRarityVisualsSO visuals)
    {
        Reward = reward;
        HasData = reward != null;

        if (!HasData)
        {
            Clear();
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = reward.Icon;
            iconImage.enabled = reward.Icon != null;
        }

        if (nameText != null)
            nameText.text = reward.DisplayName;

        if (descriptionText != null)
            descriptionText.text = reward.Description;

        EItemRarity rarity = reward.ItemRarity;
        if (rarityText != null)
            rarityText.text = visuals != null ? visuals.GetLabel(rarity) : string.Empty;

        if (frontBgImage != null && visuals != null)
            frontBgImage.color = visuals.GetFrontBgColor(rarity);

        SetSelected(false);
        gameObject.SetActive(true);
    }

    public void Clear()
    {
        Reward = null;
        HasData = false;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (rarityText != null)
            rarityText.text = string.Empty;

        if (nameText != null)
            nameText.text = string.Empty;

        if (descriptionText != null)
            descriptionText.text = string.Empty;

        SetSelected(false);
        gameObject.SetActive(false);
    }

    public void SetSelected(bool isSelected)
    {
        PlayScaleTween(isSelected && IsSelectable);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsSelectable)
            return;

        Hovered?.Invoke(this);
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
            iconImage = FindChildComponentByName<Image>("Icon");

        if (nameText == null)
            nameText = FindChildComponentByName<TMP_Text>("Name Text");

        if (rarityText == null)
            rarityText = FindChildComponentByName<TMP_Text>("Rarity Text");

        if (descriptionText == null)
            descriptionText = FindChildComponentByName<TMP_Text>("Description Text");

        if (frontBgImage == null)
        {
            Transform front = transform.Find("SelectCard Front");
            if (front != null)
                frontBgImage = FindChildComponentByName<Image>(front, "Bg");
        }
    }

    private void PlayScaleTween(bool isSelected)
    {
        if (scaleTarget == null)
            return;

        scaleTween?.Kill();

        Vector3 targetScale = isSelected
            ? normalScale * selectedScale
            : normalScale;

        scaleTween = scaleTarget
            .DOScale(targetScale, scaleTweenDuration)
            .SetEase(scaleTweenEase)
            .SetUpdate(true);
    }

    private void KillScaleTween()
    {
        scaleTween?.Kill();
        scaleTween = null;
    }

    private T FindChildComponentByName<T>(string childName) where T : Component
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name != childName)
                continue;

            if (child.TryGetComponent(out T component))
                return component;
        }

        return null;
    }

    private static T FindChildComponentByName<T>(Transform root, string childName) where T : Component
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name != childName)
                continue;

            if (child.TryGetComponent(out T component))
                return component;
        }

        return null;
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
