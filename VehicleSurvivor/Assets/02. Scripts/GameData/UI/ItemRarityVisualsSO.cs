using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemRarityVisualEntry
{
    [SerializeField] private EItemRarity rarity;
    [Tooltip("등급 표시 이름 (예: 일반, 고급)")]
    [SerializeField] private string displayLabel;
    [Tooltip("SelectCard Front/Bg에 적용할 등급 색상")]
    [SerializeField] private Color frontBg = new(0.145f, 0.153f, 0.22f, 1f);

    public EItemRarity Rarity => rarity;
    public string DisplayLabel => displayLabel;
    public Color FrontBg => frontBg;
}

[CreateAssetMenu(
    fileName = "ItemRarityVisuals",
    menuName = "GameData/UI/ItemRarityVisuals")]
public class ItemRarityVisualsSO : ScriptableObject
{
    private static readonly Color DefaultFrontBg = new(0.145f, 0.153f, 0.22f, 1f);

    [Tooltip("등급별 표시 라벨과 프레임 색상")]
    [SerializeField] private List<ItemRarityVisualEntry> entries = new();

    public string GetLabel(EItemRarity rarity)
    {
        if (TryGetEntry(rarity, out ItemRarityVisualEntry entry))
            return entry.DisplayLabel;

        return string.Empty;
    }

    public Color GetFrontBgColor(EItemRarity rarity)
    {
        if (TryGetEntry(rarity, out ItemRarityVisualEntry entry))
            return entry.FrontBg;

        if (rarity != EItemRarity.Common &&
            TryGetEntry(EItemRarity.Common, out ItemRarityVisualEntry fallback))
            return fallback.FrontBg;

        return DefaultFrontBg;
    }

    private bool TryGetEntry(EItemRarity rarity, out ItemRarityVisualEntry entry)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].Rarity == rarity)
            {
                entry = entries[i];
                return true;
            }
        }

        entry = null;
        return false;
    }
}
