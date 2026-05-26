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
            Debug.LogError($"[LoadoutSlot] {name}의 Icon Image가 할당되지 않았습니다.", this);

        if (levelText == null)
            Debug.LogError($"[LoadoutSlot] {name}의 Level Text가 할당되지 않았습니다.", this);
    }

    public void Bind(Sprite icon, int level)
    {
        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        bool showLevel = level > 0;

        if (levelText != null)
        {
            levelText.gameObject.SetActive(showLevel);
            levelText.text = showLevel ? $"LV {level}" : string.Empty;
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
