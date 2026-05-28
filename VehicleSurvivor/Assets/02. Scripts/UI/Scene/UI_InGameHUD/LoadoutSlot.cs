using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadoutSlot : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private bool hideSlotWhenEmpty;

    private void Awake()
    {
        if (iconImage == null)
            Debug.LogError($"[LoadoutSlot] {name}??Icon Imageê°€ ? ë‹¹?˜ì? ?Šì•˜?µë‹ˆ??", this);

        if (levelText == null)
            Debug.LogError($"[LoadoutSlot] {name}??Level Textê°€ ? ë‹¹?˜ì? ?Šì•˜?µë‹ˆ??", this);
    }

    public void Bind(Sprite icon, int level)
    {
        Bind(icon, level > 0 ? $"LV {level}" : string.Empty);
    }

    public void Bind(Sprite icon, string label)
    {
        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        bool showLevel = !string.IsNullOrWhiteSpace(label);

        if (levelText != null)
        {
            levelText.gameObject.SetActive(showLevel);
            levelText.text = showLevel ? label : string.Empty;
        }
    }

    public void ClearEmpty()
    {
        if (hideSlotWhenEmpty)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (levelText != null)
        {
            levelText.text = string.Empty;
            levelText.gameObject.SetActive(false);
        }
    }
}
