using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ItemDefinition",
    menuName = "GameData/Item/Item Definition")]
public class ItemDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string itemId;
    [SerializeField] private string itemName;
    [TextArea]
    [SerializeField] private string description;

    [Header("Category")]
    [SerializeField] private EItemCategory itemCategory;
    [SerializeField] private EItemRarity rarity;

    [Header("UI")]
    [SerializeField] private Sprite icon;

    [Header("Rule")]
    [SerializeField] private bool isOneTimeUse;

    [Header("Effect")]
    [SerializeField] private List<StatModifierData> statModifiers = new();

    public string ItemId => itemId;
    public string ItemName => itemName;
    public string Description => description;

    public EItemCategory ItemCategory => itemCategory;
    public EItemRarity Rarity => rarity;

    public Sprite Icon => icon;

    public bool IsOneTimeUse => isOneTimeUse;
    public bool IsSpecialHandItem => itemCategory == EItemCategory.SpecialHandItem;

    public IReadOnlyList<StatModifierData> StatModifiers => statModifiers;
}