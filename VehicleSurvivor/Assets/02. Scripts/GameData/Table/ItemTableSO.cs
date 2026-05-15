using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ItemTable",
    menuName = "GameData/Table/Item Table")]
public class ItemTableSO : ScriptableObject
{
    [SerializeField] private List<ItemDefinitionSO> items = new();

    public IReadOnlyList<ItemDefinitionSO> Items => items;

    public ItemDefinitionSO GetById(string itemId)
    {
        return items.Find(item => item.ItemId == itemId);
    }

    public List<ItemDefinitionSO> GetByCategory(EItemCategory category)
    {
        return items.FindAll(item => item.ItemCategory == category);
    }

    public List<ItemDefinitionSO> GetByRarity(EItemRarity rarity)
    {
        return items.FindAll(item => item.Rarity == rarity);
    }
}