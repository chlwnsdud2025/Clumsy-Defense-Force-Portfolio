using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SkillTable",
    menuName = "GameData/Table/Skill Table")]
public class SkillTableSO : ScriptableObject
{
    [SerializeField] private List<SkillDefinitionSO> skills = new();

    public IReadOnlyList<SkillDefinitionSO> Skills => skills;

    public SkillDefinitionSO GetById(string skillId)
    {
        return skills.Find(skill => skill.SkillId == skillId);
    }

    public List<SkillDefinitionSO> GetByCategory(ESkillCategory category)
    {
        return skills.FindAll(skill => skill.SkillCategory == category);
    }
}