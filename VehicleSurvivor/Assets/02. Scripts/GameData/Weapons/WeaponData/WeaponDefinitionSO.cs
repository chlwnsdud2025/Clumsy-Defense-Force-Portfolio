using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponDefinition",
    menuName = "TeamProject/Weapon/Weapon Definition"
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
    [SerializeField] private string requiredPassiveId;

    public string WeaponId => weaponId;
    public string WeaponName => weaponName;
    public string Description => description;
    public Sprite Icon => icon;
    public GameObject WeaponPrefab => weaponPrefab;
    public WeaponType WeaponType => weaponType;
    public IReadOnlyList<string> Tags => tags;
    public IReadOnlyList<WeaponLevelData> LevelDataList => levelDataList;
    public string EvolutionWeaponId => evolutionWeaponId;
    public string RequiredPassiveId => requiredPassiveId;

    public int MaxLevel => levelDataList.Count;

    public WeaponLevelData GetLevelData(int level)
    {
        if (levelDataList == null || levelDataList.Count == 0)
        {
            Debug.LogError($"[WeaponDefinitionSO] {weaponName}의 LevelData가 비어 있습니다.");
            return null;
        }

        int index = Mathf.Clamp(level - 1, 0, levelDataList.Count - 1);
        return levelDataList[index];
    }

    public bool HasEvolution()
    {
        return string.IsNullOrWhiteSpace(evolutionWeaponId) == false;
    }
}