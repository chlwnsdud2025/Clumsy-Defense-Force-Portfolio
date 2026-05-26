using System;
using Cysharp.Threading.Tasks;
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

    [Header("카드 면")]
    [SerializeField] private GameObject frontObject;
    [SerializeField] private GameObject backObject;
    [SerializeField] private RectTransform flipTarget;

    [Header("상호작용")]
    [SerializeField] private Button button;
    [SerializeField] private RectTransform scaleTarget;

    [Header("Selection Effect")]
    [SerializeField] private float selectedScale = 1.05f;
    [SerializeField] private float scaleTweenDuration = 0.15f;
    [SerializeField] private Ease scaleTweenEase = Ease.OutQuad;

    [Header("Flip Effect")]
    [SerializeField] private float flipDuration = 0.35f;
    [SerializeField] private Ease flipEase = Ease.InOutQuad;

    public float ScaleTweenDuration => scaleTweenDuration;

    public event Action<SelectCardFrame> Hovered;
    public event Action<SelectCardFrame> Clicked;

    public RewardOption Reward { get; private set; }
    public bool HasData { get; private set; }
    public EItemRarity ItemRarity => Reward?.ItemRarity ?? EItemRarity.None;
    public bool IsFaceUp { get; private set; } = true;
    public bool IsInteractionEnabled { get; private set; } = true;
    public bool IsSelectable => gameObject.activeInHierarchy && HasData && IsInteractionEnabled;

    private Vector3 normalScale;
    private Tween scaleTween;
    private Tween flipTween;
    private Tween anticipationTween;

    private void Awake()
    {
        ResolveReferences();

        if (scaleTarget == null)
            scaleTarget = transform as RectTransform;

        if (flipTarget == null)
            flipTarget = scaleTarget;

        normalScale = scaleTarget != null ? scaleTarget.localScale : Vector3.one;

        RegisterButtonClick();
        SetFaceUp(true);
        SetSelected(false);
    }

    private void OnDisable()
    {
        KillScaleTween();
        KillFlipTween();
        KillAnticipationTween();

        if (scaleTarget != null)
        {
            scaleTarget.localScale = normalScale;
            scaleTarget.localRotation = Quaternion.identity;
        }
    }

    private void OnDestroy()
    {
        KillScaleTween();
        KillFlipTween();
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

    /// <summary>0단계 - 연출 시작 전: 뒷면 표시, Scale 0, 입력 비활성화</summary>
    public void PrepareIntroBackFace()
    {
        KillScaleTween();
        KillFlipTween();
        KillAnticipationTween();

        if (scaleTarget != null)
        {
            scaleTarget.localScale = Vector3.zero;
            scaleTarget.localRotation = Quaternion.identity;
        }

        SetFaceUp(false);
        SetInteractionEnabled(false);
    }

    /// <summary>1단계 - Scale 0에서 원래 크기로 등장</summary>
    public async UniTask PlayIntroScaleAsync(float duration, Ease ease)
    {
        if (scaleTarget == null)
            return;

        KillScaleTween();

        scaleTarget.localScale = Vector3.zero;

        scaleTween = scaleTarget
            .DOScale(normalScale, duration)
            .SetEase(ease)
            .SetUpdate(true);
        await TweenAwaiter.Await(scaleTween);
        scaleTween = null;
    }

    /// <summary>4-1단계 - 멈칫(pause) 후 카드 흔들림</summary>
    public async UniTask PlayAnticipationShakeAsync(float pauseDuration, float shakeDuration, float shakeStrength)
    {
        // 4-1a: 멈칫
        if (pauseDuration > 0f)
            await UniTask.Delay(TimeSpan.FromSeconds(pauseDuration), ignoreTimeScale: true);

        RectTransform target = scaleTarget != null ? scaleTarget : transform as RectTransform;
        if (target == null)
            return;

        KillAnticipationTween();
        target.localRotation = Quaternion.identity;

        // 4-1b: 흔들림
        anticipationTween = target
            .DOPunchRotation(new Vector3(0f, 0f, shakeStrength), shakeDuration, 10, 0.6f)
            .SetUpdate(true);
        await TweenAwaiter.Await(anticipationTween);
        anticipationTween = null;
        target.localRotation = Quaternion.identity;
    }

    /// <summary>3·4-3단계 - 카드 뒤집기 (scaleX 접기 → 앞면 전환 → 펼치기)</summary>
    public async UniTask PlayFlipRevealAsync(bool enableInteractionOnComplete = true)
    {
        if (IsFaceUp)
        {
            if (enableInteractionOnComplete)
                SetInteractionEnabled(true);
            return;
        }

        RectTransform target = flipTarget != null ? flipTarget : scaleTarget;
        if (target == null)
        {
            SetFaceUp(true);
            if (enableInteractionOnComplete)
                SetInteractionEnabled(true);
            return;
        }

        KillFlipTween();

        float halfDuration = flipDuration * 0.5f;
        Vector3 collapsedScale = new(0f, normalScale.y, normalScale.z);

        // 뒤집기 전반: scaleX 1 → 0
        flipTween = target
            .DOScale(collapsedScale, halfDuration)
            .SetEase(flipEase)
            .SetUpdate(true);
        await TweenAwaiter.Await(flipTween);

        // 중간: 뒷면 → 앞면 전환
        SetFaceUp(true);
        target.localScale = collapsedScale;

        // 뒤집기 후반: scaleX 0 → 1
        flipTween = target
            .DOScale(normalScale, halfDuration)
            .SetEase(flipEase)
            .SetUpdate(true);
        await TweenAwaiter.Await(flipTween);

        flipTween = null;

        if (enableInteractionOnComplete)
            SetInteractionEnabled(true);
    }

    public void SetInteractionEnabled(bool enabled)
    {
        IsInteractionEnabled = enabled;

        if (button != null)
            button.interactable = enabled;
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

        if (frontObject == null)
        {
            Transform front = transform.Find("SelectCard Front");
            if (front != null)
                frontObject = front.gameObject;
        }

        if (backObject == null)
        {
            Transform back = transform.Find("SelectCard Back");
            if (back != null)
                backObject = back.gameObject;
        }
    }

    private void SetFaceUp(bool faceUp)
    {
        IsFaceUp = faceUp;

        if (frontObject != null)
            frontObject.SetActive(faceUp);

        if (backObject != null)
            backObject.SetActive(!faceUp);
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

    private void KillFlipTween()
    {
        flipTween?.Kill();
        flipTween = null;
    }

    private void KillAnticipationTween()
    {
        anticipationTween?.Kill();
        anticipationTween = null;
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

internal static class TweenAwaiter
{
    public static UniTask Await(Tween tween)
    {
        if (tween == null || !tween.IsActive())
            return UniTask.CompletedTask;

        UniTaskCompletionSource completionSource = new();
        tween.OnComplete(() => completionSource.TrySetResult());
        tween.OnKill(() => completionSource.TrySetResult());
        return completionSource.Task;
    }
}
