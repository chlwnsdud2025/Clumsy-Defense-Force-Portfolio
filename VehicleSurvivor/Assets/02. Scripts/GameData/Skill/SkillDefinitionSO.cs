using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SkillDefinition",
    menuName = "GameData/Skill/Skill Definition")]
public class SkillDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string skillId;
    [SerializeField] private string skillName;
    [TextArea]
    [SerializeField] private string description;

    [Header("Category")]
    [SerializeField] private ESkillCategory skillCategory;
    [SerializeField] private ESkillTag tags;

    [Header("UI")]
    [SerializeField] private Sprite icon;

    [Header("Level")]
    [SerializeField] private int maxLevel = 5;
    [SerializeField] private List<SkillLevelData> levelDataList = new();

    [Header("Evolution")]
    [SerializeField] private bool canEvolve = true;
    [SerializeField] private string evolvedSkillName;
    [TextArea]
    [SerializeField] private string evolvedDescription;
    [SerializeField] private EStatType requiredPassiveStatType;

    [Header("Runtime")]
    [SerializeField] private GameObject skillPrefab;
    [SerializeField] private SkillDefinitionSO evolvedSkillDefinition;

    public string SkillId => skillId;
    public string SkillName => skillName;
    public string Description => description;

    public ESkillCategory SkillCategory => skillCategory;
    public ESkillTag Tags => tags;

    public Sprite Icon => icon;

    public int MaxLevel => maxLevel;
    public IReadOnlyList<SkillLevelData> LevelDataList => levelDataList;

    public bool CanEvolve => canEvolve;
    public string EvolvedSkillName => evolvedSkillName;
    public string EvolvedDescription => evolvedDescription;
    public EStatType RequiredPassiveStatType => requiredPassiveStatType;

    public GameObject SkillPrefab => skillPrefab;
    public SkillDefinitionSO EvolvedSkillDefinition => evolvedSkillDefinition;

    public SkillLevelData GetLevelData(int level)
    {
        if (level <= 0 || level > levelDataList.Count)
        {
            Debug.LogError($"[{name}] 잘못된 스킬 레벨 요청: {level}");
            return null;
        }

        return levelDataList[level - 1];
    }
}