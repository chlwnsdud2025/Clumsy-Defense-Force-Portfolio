using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BadgeTable",
    menuName = "GameData/Table/Badge Table")]
public class BadgeTableSO : ScriptableObject
{
    [SerializeField] private List<BadgeDefinitionSO> badges = new();

    public IReadOnlyList<BadgeDefinitionSO> Badges => badges;

    public BadgeDefinitionSO GetById(string badgeId)
    {
        return badges.Find(badge => badge.BadgeId == badgeId);
    }

    public BadgeDefinitionSO GetByStatType(EStatType statType)
    {
        return badges.Find(badge => badge.StatType == statType);
    }
}