using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PassiveDefinition",
    menuName = "GameData/Skill/Passive Definition")]
public class PassiveDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string passiveId;
    [SerializeField] private string passiveName;
    [TextArea]
    [SerializeField] private string description;

    [Header("UI")]
    [SerializeField] private Sprite icon;

    [Header("Stat")]
    [SerializeField] private EStatType statType;
    [SerializeField] private int maxLevel = 5;
    [SerializeField] private List<StatModifierData> levelModifiers = new();

    public string PassiveId => passiveId;
    public string PassiveName => passiveName;
    public string Description => description;

    public Sprite Icon => icon;

    public EStatType StatType => statType;
    public int MaxLevel => maxLevel;
    public IReadOnlyList<StatModifierData> LevelModifiers => levelModifiers;

    public StatModifierData GetModifierData(int level)
    {
        if (level <= 0 || level > levelModifiers.Count)
        {
            Debug.LogError($"[{name}] 잘못된 패시브 레벨 요청: {level}");
            return null;
        }

        return levelModifiers[level - 1];
    }
}