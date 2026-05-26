using TMPro;
using UnityEngine;

public class StatPanel : MonoBehaviour
{
    [SerializeField] private EStatType statType;
    [SerializeField] private TMP_Text valueText;

    public EStatType StatType => statType;

    private void Awake()
    {
        if (statType == EStatType.None)
            Debug.LogWarning($"[StatPanel] '{name}'에 StatType이 할당되지 않았습니다.", this);

        if (valueText == null)
            Debug.LogWarning($"[StatPanel] '{name}'에 ValueText가 할당되지 않았습니다.", this);
    }

    public void SetValue(string value)
    {
        if (valueText == null)
            return;

        valueText.text = value;
    }
}
