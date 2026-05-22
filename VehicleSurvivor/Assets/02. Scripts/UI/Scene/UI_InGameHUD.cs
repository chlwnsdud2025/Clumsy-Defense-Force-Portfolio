using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_InGameHUD : UIScene
{
    private IEventBus _eventBus;
    private bool _isSubscribed;

    [Header("Player UI")]
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider boostSlider;

    [Header("Game Info UI")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI killCountText;
    [SerializeField] private TextMeshProUGUI totalTimerText;
    [SerializeField] private TextMeshProUGUI waveTimerText;

    [Header("Wave Timer")]
    [SerializeField] private RectTransform waveTimerRect;
    [SerializeField] private Color waveTimerNormalTextColor = Color.white;
    [SerializeField] private Color waveTimerNightmareTextColor = new(1f, 0.27f, 0.27f);
    [SerializeField] private float waveTimerNightmarePunchPower = 0.15f;
    [SerializeField] private float waveTimerStyleTweenDuration = 0.3f;

    [Header("Game UI")]
    [SerializeField] private RectTransform pauseButtonRect;

    [Header("HP Tween")]
    [SerializeField] private float hpTweenDuration = 0.25f;
    [SerializeField] private Ease hpTweenEase = Ease.OutQuad;

    [Header("EXP Tween")]
    [SerializeField] private float expTweenDuration = 0.25f;
    [SerializeField] private Ease expTweenEase = Ease.OutQuad;

    [Header("Boost Tween")]
    [SerializeField] private float boostTweenDuration = 0.25f;
    [SerializeField] private Ease boostTweenEase = Ease.OutQuad;

    [Header("HP Warning")]
    [SerializeField] private float hpWarningThreshold = 0.2f;
    [SerializeField] private float hpWarningFadeDuration = 0.35f;
    [SerializeField] private float hpWarningTargetAlpha = 0.35f;
    [SerializeField] private Ease hpWarningEase = Ease.Linear;

    [Header("Pause Button Tween")]
    [SerializeField] private float pauseButtonPunchPower = 0.12f;
    [SerializeField] private float pauseButtonTweenDuration = 0.15f;

    private Tween hpTween;
    private Tween expTween;
    private Tween boostTween;
    private Tween hpWarningTween;
    private Tween pauseButtonTween;
    private Tween waveTimerStyleTween;
    private Tween waveTimerPunchTween;

    private bool _isWaveNightmare;

    private void Awake()
    {
        if (waveTimerRect == null && waveTimerText != null)
            waveTimerRect = waveTimerText.rectTransform;
    }

    public override void OnShow()
    {
        base.OnShow();
        SubscribeEvents();
    }

    public override void OnHide()
    {
        UnsubscribeEvents();
        KillTweens();
        base.OnHide();
    }

    public void InitializeForCharacter(CharacterDefinitionSO character)
    {
        if (character == null)
        {
            UpdateCharacterName(string.Empty);
            UpdateHpGauge(0, 1);
            UpdateExpGauge(0, 1);
            UpdateLevelUI(1);
            UpdateBoostGauge(0, 1);
            UpdateGoldUI(0);
            UpdateKillUI(0);
            UpdateTotalTimerUI(0f);
            UpdateWaveTimerUI(0f, false);
            return;
        }

        UpdateCharacterName(character.DisplayName);

        int maxHp = Mathf.RoundToInt(character.Combat.MaxHealth);
        UpdateHpGauge(maxHp, maxHp);

        UpdateExpGauge(0, 1);
        UpdateLevelUI(1);
        UpdateBoostGauge(0, 1);
        UpdateGoldUI(0);
        UpdateKillUI(0);
        UpdateTotalTimerUI(0f);
        UpdateWaveTimerUI(0f, false);
    }

    private void SubscribeEvents()
    {
        if (_isSubscribed)
            return;

        if (_eventBus == null)
            _eventBus = ServiceLocator.Get<IEventBus>();

        _eventBus.Subscribe<HpChangedEvent>(OnHpChanged);
        _eventBus.Subscribe<ExpChangedEvent>(OnExpChanged);
        _eventBus.Subscribe<LevelChangedEvent>(OnLevelChanged);
        _eventBus.Subscribe<KillCountEvent>(OnKillChanged);
        _eventBus.Subscribe<TimeChangeEvent>(OnTimeChanged);
        _eventBus.Subscribe<WaveTimeChangedEvent>(OnWaveTimeChanged);
        _eventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);

        _isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!_isSubscribed)
            return;

        if (_eventBus != null)
        {
            _eventBus.Unsubscribe<HpChangedEvent>(OnHpChanged);
            _eventBus.Unsubscribe<ExpChangedEvent>(OnExpChanged);
            _eventBus.Unsubscribe<LevelChangedEvent>(OnLevelChanged);
            _eventBus.Unsubscribe<KillCountEvent>(OnKillChanged);
            _eventBus.Unsubscribe<TimeChangeEvent>(OnTimeChanged);
            _eventBus.Unsubscribe<WaveTimeChangedEvent>(OnWaveTimeChanged);
            _eventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
        }

        _isSubscribed = false;
    }

    public void UpdateCharacterName(string characterName)
    {
        if (characterNameText != null)
            characterNameText.text = characterName;
    }

    public void UpdateHpGauge(int current, int max)
    {
        float ratio = max > 0 ? (float)current / max : 0f;
        ratio = Mathf.Clamp01(ratio);

        if (hpSlider != null)
        {
            hpTween?.Kill();
            hpTween = hpSlider
                .DOValue(ratio, hpTweenDuration)
                .SetEase(hpTweenEase);
        }

        if (hpText != null)
            hpText.text = $"{current} / {max}";

        if (ratio < hpWarningThreshold)
            StartWarningHp();
        else
            StopWarningHp();
    }

    public void UpdateBoostGauge(int current, int max)
    {
        float ratio = max > 0 ? (float)current / max : 0f;
        ratio = Mathf.Clamp01(ratio);

        if (boostSlider != null)
        {
            boostTween?.Kill();
            boostTween = boostSlider
                .DOValue(ratio, boostTweenDuration)
                .SetEase(boostTweenEase);
        }
    }

    public void UpdateExpGauge(int current, int max)
    {
        float ratio = max > 0 ? (float)current / max : 0f;
        ratio = Mathf.Clamp01(ratio);

        if (expSlider != null)
        {
            expTween?.Kill();
            expTween = expSlider
                .DOValue(ratio, expTweenDuration)
                .SetEase(expTweenEase);
        }
    }

    public void UpdateLevelUI(int level)
    {
        if (levelText != null)
            levelText.text = $"Lv. {level}";
    }

    public void UpdateGoldUI(int gold)
    {
        if (goldText != null)
            goldText.text = gold.ToString("N0");
    }

    public void UpdateKillUI(int kills)
    {
        if (killCountText != null)
            killCountText.text = kills.ToString("N0");
    }

    public void UpdateTotalTimerUI(float elapsed)
    {
        if (totalTimerText == null)
            return;

        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);
        totalTimerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void UpdateWaveTimerUI(float displayTime, bool isNightmare)
    {
        if (waveTimerText == null)
            return;

        float clamped = Mathf.Max(0f, displayTime);
        int minutes = Mathf.FloorToInt(clamped / 60f);
        int seconds = Mathf.FloorToInt(clamped % 60f);
        waveTimerText.text = isNightmare
            ? $"+{minutes:00}:{seconds:00}"
            : $"{minutes:00}:{seconds:00}";

        ApplyWaveTimerStyle(isNightmare);
    }

    private void ApplyWaveTimerStyle(bool isNightmare)
    {
        if (_isWaveNightmare == isNightmare)
            return;

        bool enteringNightmare = isNightmare && !_isWaveNightmare;
        _isWaveNightmare = isNightmare;

        waveTimerStyleTween?.Kill();
        waveTimerPunchTween?.Kill();

        Color targetTextColor = isNightmare ? waveTimerNightmareTextColor : waveTimerNormalTextColor;

        if (waveTimerText != null)
        {
            waveTimerStyleTween = waveTimerText
                .DOColor(targetTextColor, waveTimerStyleTweenDuration)
                .SetEase(Ease.OutQuad);
        }

        if (enteringNightmare && waveTimerRect != null)
        {
            waveTimerRect.localScale = Vector3.one;
            waveTimerPunchTween = waveTimerRect
                .DOPunchScale(Vector3.one * waveTimerNightmarePunchPower, waveTimerStyleTweenDuration);
        }
        else if (waveTimerRect != null)
        {
            waveTimerRect.localScale = Vector3.one;
        }
    }

    private void OnHpChanged(HpChangedEvent e)
    {
        UpdateHpGauge(e.Current, e.Max);
    }

    private void OnExpChanged(ExpChangedEvent e)
    {
        UpdateExpGauge(e.Current, e.Required);
    }

    private void OnLevelChanged(LevelChangedEvent e)
    {
        UpdateLevelUI(e.Level);
    }

    private void OnKillChanged(KillCountEvent e)
    {
        UpdateKillUI(e.KillCount);
    }

    private void OnTimeChanged(TimeChangeEvent e)
    {
        UpdateTotalTimerUI(e.Time);
    }

    private void OnWaveTimeChanged(WaveTimeChangedEvent e)
    {
        UpdateWaveTimerUI(e.DisplayTime, e.IsNightmare);
    }

    private void OnGoldChanged(GoldChangedEvent e)
    {
        UpdateGoldUI(e.Gold);
    }

    private void StartWarningHp()
    {
        if (hpText == null)
            return;

        if (hpWarningTween != null && hpWarningTween.IsActive())
            return;

        hpWarningTween = hpText
            .DOFade(hpWarningTargetAlpha, hpWarningFadeDuration)
            .SetEase(hpWarningEase)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopWarningHp()
    {
        hpWarningTween?.Kill();
        hpWarningTween = null;

        if (hpText != null)
            hpText.alpha = 1f;
    }

    private void KillTweens()
    {
        hpTween?.Kill();
        expTween?.Kill();
        boostTween?.Kill();
        hpWarningTween?.Kill();
        pauseButtonTween?.Kill();
        waveTimerStyleTween?.Kill();
        waveTimerPunchTween?.Kill();

        hpTween = null;
        expTween = null;
        boostTween = null;
        hpWarningTween = null;
        pauseButtonTween = null;
        waveTimerStyleTween = null;
        waveTimerPunchTween = null;
    }

    private void PlayPauseButtonTween()
    {
        if (pauseButtonRect == null)
            return;

        pauseButtonTween?.Kill();
        pauseButtonRect.localScale = Vector3.one;
        pauseButtonTween = pauseButtonRect.DOPunchScale(Vector3.one * pauseButtonPunchPower, pauseButtonTweenDuration);
    }

    public void OnClickPausePopup()
    {
        PlayPauseButtonTween();
        UIManager.Instance.ShowPopup(EPopupUIType.Pause);
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        KillTweens();
    }
}
