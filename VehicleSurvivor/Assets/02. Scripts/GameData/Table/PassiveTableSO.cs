using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PassiveTable",
    menuName = "GameData/Table/Passive Table")]
public class PassiveTableSO : ScriptableObject
{
    [SerializeField] private List<PassiveDefinitionSO> passives = new();

    public IReadOnlyList<PassiveDefinitionSO> Passives => passives;

    public PassiveDefinitionSO GetById(string passiveId)
    {
        return passives.Find(passive => passive.PassiveId == passiveId);
    }

    public PassiveDefinitionSO GetByStatType(EStatType statType)
    {
        return passives.Find(passive => passive.StatType == statType);
    }
}