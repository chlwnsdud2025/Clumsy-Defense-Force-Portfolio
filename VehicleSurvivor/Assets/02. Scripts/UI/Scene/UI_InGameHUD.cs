using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_InGameHUD : UIScene
{
    private IEventBus _eventBus;
    [Header("Player UI")]
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private Slider boostSlider;
    [Header("Game Info UI")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI killCountText;
    [SerializeField] private TextMeshProUGUI timerText;
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

    public override void OnShow()
    {
        if (_eventBus == null)
        {
            _eventBus = ServiceLocator.Get<IEventBus>();
        }
        _eventBus.Subscribe<HpChangedEvent>(OnHpChanged);
        _eventBus.Subscribe<KillCountEvent>(OnKillChanged);
        _eventBus.Subscribe<TimeChangeEvent>(OnTimeChanged);
        _eventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
    }
    public override void OnHide()
    {
        if (_eventBus != null)
        {
            _eventBus.Unsubscribe<HpChangedEvent>(OnHpChanged);
            _eventBus.Unsubscribe<KillCountEvent>(OnKillChanged);
            _eventBus.Unsubscribe<TimeChangeEvent>(OnTimeChanged);
            _eventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
        }

        KillTweens();//이벤트 버스가 없어도 킬할수있게
    }
    public void UpdateCharacterName(string characterName)
    {
        if (characterNameText != null)
        {
            characterNameText.text = characterName;
        }
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
        {
            hpText.text = $"{current} / {max}";
        }
        if (ratio < hpWarningThreshold)
        {
            StartWarningHp();
        }
        else
        {
            StopWarningHp();
        }
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
    public void UpdateGoldUI(int gold)
    {
        if (goldText != null)
        {
            goldText.text = gold.ToString("N0");
        }
    }
    public void UpdateKillUI(int kills)
    {
        if (killCountText != null)
        {
            killCountText.text = kills.ToString("N0");
        }
    }
    public void UpdateTimerUI(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        if(timerText != null)
        {
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    private void OnHpChanged(HpChangedEvent e)
    {
        UpdateHpGauge(e.Current, e.Max);
    }
    private void OnKillChanged(KillCountEvent e)
    {
        UpdateKillUI(e.KillCount);
    }

    private void OnTimeChanged(TimeChangeEvent e)
    {
        UpdateTimerUI(e.Time);
    }

    private void OnGoldChanged(GoldChangedEvent e)
    {
        UpdateGoldUI(e.Gold);
    }

    private void StartWarningHp()
    {
        if (hpText == null) return;

        if (hpWarningTween != null &&
            hpWarningTween.IsActive())
        {
            return;
        } // 가독성좋다해서
        hpWarningTween = hpText
            .DOFade(hpWarningTargetAlpha, hpWarningFadeDuration)
            .SetEase(hpWarningEase)
            .SetLoops(-1, LoopType.Yoyo);
    }
    private void StopWarningHp()
    {
        hpWarningTween?.Kill();
        hpWarningTween = null; // Kill 후 참조 정리

        if (hpText != null)
        {
            hpText.alpha = 1f;
        }
    }
    private void OnDestroy()
    {
        OnHide();
    }
    private void KillTweens()
    {
        hpTween?.Kill();
        expTween?.Kill();
        boostTween?.Kill();
        hpWarningTween?.Kill();
        pauseButtonTween?.Kill();
        //kill 후 참조처리
        hpTween = null;
        expTween = null;
        boostTween = null;
        hpWarningTween = null;
        pauseButtonTween = null;
    }

    private void PlayPauseButtonTween()
    {
        if (pauseButtonRect == null) return;
        pauseButtonTween?.Kill();
        pauseButtonRect.localScale = Vector3.one;
        pauseButtonTween = pauseButtonRect.DOPunchScale(Vector3.one * pauseButtonPunchPower, pauseButtonTweenDuration);
    }

    public void OnClickPausePopup()
    {
        PlayPauseButtonTween();
        UIManager.Instance.ShowPopup(EPopupUIType.Pause);
    }
}
