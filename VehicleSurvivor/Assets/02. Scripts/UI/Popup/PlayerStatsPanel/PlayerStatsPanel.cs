using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PlayerStatsPanel : MonoBehaviour
{
    [Header("스탯 패널")]
    [SerializeField] private StatPanel[] statPanels;

    [Header("진입 애니메이션")]
    [SerializeField] private RectTransform tweenTarget;
    [SerializeField] private float enterDuration = 0.35f;
    [SerializeField] private Ease enterEase = Ease.OutBack;

    private readonly Dictionary<EStatType, StatPanel> panelByStatType = new();

    private PlayerRuntime boundPlayer;
    private Vector3 normalScale = Vector3.one;
    private Tween enterTween;

    private bool isInitialized;

    private void Awake()
    {
        if (tweenTarget == null)
            tweenTarget = gameObject.GetComponent<RectTransform>();

        normalScale = tweenTarget.localScale;
    }

    private void OnDisable()
    {
        StopEnterAnimation();
        Unbind();
    }

    private void OnDestroy()
    {
        StopEnterAnimation();
    }

    public void Configure(float duration, Ease ease)
    {
        enterDuration = duration;
        enterEase = ease;
    }

    public void PlayEnterAnimation()
    {
        StopEnterAnimation();

        if (tweenTarget == null)
            return;

        tweenTarget.localScale = Vector3.zero;
        enterTween = tweenTarget
            .DOScale(normalScale, enterDuration)
            .SetEase(enterEase)
            .SetUpdate(true);
    }

    public void StopEnterAnimation()
    {
        if (enterTween != null && enterTween.IsActive())
            enterTween.Kill();

        enterTween = null;

        if (tweenTarget != null)
            tweenTarget.localScale = normalScale;
    }

    private void EnsureInitialized()
    {
        if (isInitialized)
            return;

        if (statPanels == null || statPanels.Length == 0)
            statPanels = GetComponentsInChildren<StatPanel>(true);

        panelByStatType.Clear();

        foreach (StatPanel panel in statPanels)
        {
            if (panel == null || panel.StatType == EStatType.None)
                continue;

            panelByStatType[panel.StatType] = panel;
        }

        isInitialized = true;
    }

    public void Bind(PlayerRuntime player)
    {
        EnsureInitialized();

        if (boundPlayer == player)
        {
            RefreshAll();
            return;
        }

        Unbind();

        boundPlayer = player;
        if (boundPlayer == null)
            return;

        boundPlayer.Stats.OnStatChanged += HandleStatChanged;
        RefreshAll();
    }

    public void Unbind()
    {
        if (boundPlayer == null)
            return;

        boundPlayer.Stats.OnStatChanged -= HandleStatChanged;
        boundPlayer = null;
    }

    private void HandleStatChanged(EStatType statType, float oldValue, float newValue)
    {
        RefreshStat(statType);
    }

    private void RefreshAll()
    {
        if (boundPlayer == null)
            return;

        foreach (KeyValuePair<EStatType, StatPanel> entry in panelByStatType)
            RefreshStat(entry.Key);
    }

    private void RefreshStat(EStatType statType)
    {
        if (boundPlayer == null)
            return;

        if (!panelByStatType.TryGetValue(statType, out StatPanel panel))
            return;

        panel.SetValue(FormatStatValue(statType, boundPlayer));
    }

    private static string FormatStatValue(EStatType statType, PlayerRuntime player)
    {
        float value = player.Stats.GetValue(statType);

        switch (statType)
        {
            case EStatType.MaxHealth:
            case EStatType.HealthRegen:
            case EStatType.Defense:
            case EStatType.ProjectileCount:
            case EStatType.Luck:
                return Mathf.RoundToInt(value).ToString();
            case EStatType.CooldownReduction:
                return $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
            case EStatType.CriticalChance:
                return $"{Mathf.RoundToInt(value * 100f)}%";
            case EStatType.MoveSpeedMultiplier:
            case EStatType.DamageMultiplier:
            case EStatType.MagicDamageMultiplier:
            case EStatType.EvolvedWeaponDamage:
            case EStatType.CriticalDamageMultiplier:
            case EStatType.WeaponSizeMultiplier:
            case EStatType.ExpGainMultiplier:
            case EStatType.GoldGainMultiplier:
            case EStatType.PickupRangeMultiplier:
                return $"x{value:0.##}";
            default:
                return value.ToString("0.##");
        }
    }
}
