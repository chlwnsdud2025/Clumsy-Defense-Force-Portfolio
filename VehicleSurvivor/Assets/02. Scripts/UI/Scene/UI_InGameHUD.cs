using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_InGameHUD : UIScene
{
    [Header("Player UI")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private Slider boostSlider;

    UI_InGameHUD ingameHUD = UIManager.Instance.GetCurrentScene<UI_InGameHUD>();
    public void SetHp(int current, int max)
    {
        if (max <= 0) return;

        if (hpSlider != null) hpSlider.value = (float)current / max;
        if (hpText != null) hpText.text = $"{current} / {max}";
    }
    public void SetBoost(int current, int max)
    {
        if (max <= 0) return;

        if (boostSlider != null) boostSlider.value = (float)current / max;

    }
    public void SetExp(int current, int max)
    {
        if (max <= 0) return;

        if (expSlider != null) expSlider.value = (float)current / max;

    }


    public void OnClickPausePopup()
    {
        UIManager.Instance.ShowPopup(EPopupUIType.SelectCard);
    }
}
