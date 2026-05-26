using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponDefinition",
    menuName = "GameData/Weapon/Weapon Definition"
)]
public class WeaponDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string weaponId;
    [SerializeField] private string weaponName;

    [TextArea]
    [SerializeField] private string description;

    [Header("Visual")]
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject weaponPrefab;

    [Header("Type")]
    [SerializeField] private WeaponType weaponType;
    [SerializeField] private List<string> tags = new();

    [Header("Level Data")]
    [SerializeField] private List<WeaponLevelData> levelDataList = new();

    [Header("Evolution")]
    [SerializeField] private string evolutionWeaponId;
    [SerializeField] private string requiredBadgeId;

    public string WeaponId => weaponId;
    public string WeaponName => weaponName;
    public string Description => description;
    public Sprite Icon => icon;
    public GameObject WeaponPrefab => weaponPrefab;
    public WeaponType WeaponType => weaponType;
    public IReadOnlyList<string> Tags => tags;
    public IReadOnlyList<WeaponLevelData> LevelDataList => levelDataList;
    public string EvolutionWeaponId => evolutionWeaponId;
    public string RequiredBadgeId => requiredBadgeId;

    public int MaxLevel => levelDataList?.Count ?? 0;

    public WeaponLevelData GetLevelData(int level)
    {
        if (levelDataList == null || levelDataList.Count == 0)
        {
            Debug.LogError($"[WeaponDefinitionSO] {weaponName}??LevelDataê°€ ë¹„ì–´ ?ˆìŠµ?ˆë‹¤.");
            return null;
        }

        if (level < 1 || level > levelDataList.Count)
        {
            Debug.LogError($"[WeaponDefinitionSO] {weaponName}???˜ëª»???ˆë²¨ ?”ì²­: {level} / MaxLevel: {MaxLevel}");
            return null;
        }

        int index = level - 1;
        return levelDataList[index];
    }

    public string GetCurrentDescription(int currentLevel)
    {
        WeaponLevelData currentData = GetLevelData(currentLevel);

        if (currentData == null)
            return string.Empty;

        return currentData.Description;
    }

    public string GetNextLevelDescription(int currentLevel)
    {
        int nextLevel = currentLevel + 1;

        if (nextLevel > MaxLevel)
            return "ìµœë? ?ˆë²¨?…ë‹ˆ??";

        WeaponLevelData nextData = GetLevelData(nextLevel);

        if (nextData == null)
            return string.Empty;

        return nextData.Description;
    }

    public bool HasEvolution()
    {
        return string.IsNullOrWhiteSpace(evolutionWeaponId) == false;
    }
}