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

    public void UpdateCharacterName(string characterName)
    {
        if (characterNameText != null)
        {
            characterNameText.text = characterName;
        }
    }
    public void UpdateHpGauge(int current, int max)
    {

        if (hpSlider != null)
        {
            hpSlider.value = max > 0 ? (float)current / max : 0f;
        }
        if (hpText != null)
        {
            hpText.text = $"{current} / {max}";
        }
    }
    public void UpdateBoostGauge(int current, int max)
    {
        if (max <= 0) return;

        if (boostSlider != null)
        {
            boostSlider.value = (float)current / max;
        }

    }
    public void UpdateExpGauge(int current, int max)
    {
        if (max <= 0) return;

        if (expSlider != null)
        {
            expSlider.value = (float)current / max;
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

    public void OnClickPausePopup()
    {
        UIManager.Instance.ShowPopup(EPopupUIType.Pause);
    }
}
