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

    private Tween hpTween;
    private Tween expTween;
    private Tween boostTween;
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
            hpTween = hpSlider.DOValue(ratio, 0.25f).SetEase(Ease.OutQuad);
        }
        if (hpText != null)
        {
            hpText.text = $"{current} / {max}";
        }
    }
    public void UpdateBoostGauge(int current, int max)
    {
        float ratio = max > 0 ? (float)current / max : 0f;
        ratio = Mathf.Clamp01(ratio);
        if (boostSlider != null)
        {
            boostTween?.Kill();
            boostTween = boostSlider.DOValue(ratio, 0.25f).SetEase(Ease.OutQuad);
        }

    }
    public void UpdateExpGauge(int current, int max)
    {
        float ratio = max > 0 ? (float)current / max : 0f;
        ratio = Mathf.Clamp01(ratio);
        if (expSlider != null)
        {
            expTween?.Kill();
            expTween = expSlider.DOValue(ratio, 0.25f).SetEase(Ease.OutQuad);
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
    private void OnDestroy()
    {
        hpTween?.Kill();
        expTween?.Kill();
        boostTween?.Kill();
    }
    public void OnClickPausePopup()
    {
        UIManager.Instance.ShowPopup(EPopupUIType.Pause);
    }
}
