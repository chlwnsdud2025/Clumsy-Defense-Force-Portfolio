using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;

    public bool IsOccupied { get; private set; }

    public void Show(string displayName, int current, int max)
    {
        IsOccupied = true;
        gameObject.SetActive(true);

        if (nameText != null)
            nameText.text = displayName;

        UpdateHp(current, max);
    }

    public void UpdateHp(int current, int max)
    {
        if (!IsOccupied)
            return;

        float ratio = max > 0 ? (float)current / max : 0f;
        ratio = Mathf.Clamp01(ratio);

        if (hpSlider != null)
            hpSlider.value = ratio;

        if (hpText != null)
            hpText.text = $"{current}/{max}";
    }

    public void Hide()
    {
        IsOccupied = false;

        if (hpSlider != null)
            hpSlider.value = 0f;

        if (hpText != null)
            hpText.text = string.Empty;

        if (nameText != null)
            nameText.text = string.Empty;

        gameObject.SetActive(false);
    }
}
