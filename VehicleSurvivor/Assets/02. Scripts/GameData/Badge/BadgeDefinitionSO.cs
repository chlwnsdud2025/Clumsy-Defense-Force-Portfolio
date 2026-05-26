using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BadgeDefinition",
    menuName = "GameData/Badge/Badge Definition")]
public class BadgeDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string badgeId;
    [SerializeField] private string badgeName;
    [TextArea]
    [SerializeField] private string description;

    [Header("UI")]
    [SerializeField] private Sprite icon;

    [Header("Stat")]
    [SerializeField] private EStatType statType;
    [SerializeField] private int maxLevel = 5;
    [SerializeField] private List<StatModifierData> levelModifiers = new();

    public string BadgeId => badgeId;
    public string BadgeName => badgeName;
    public string Description => description;

    public Sprite Icon => icon;

    public EStatType StatType => statType;
    public int MaxLevel => maxLevel;
    public IReadOnlyList<StatModifierData> LevelModifiers => levelModifiers;

    public StatModifierData GetModifierData(int level)
    {
        if (level <= 0 || level > levelModifiers.Count)
        {
            Debug.LogError($"[{name}] ?섎せ???⑥떆釉??덈꺼 ?붿껌: {level}");
            return null;
        }

        return levelModifiers[level - 1];
    }
}
