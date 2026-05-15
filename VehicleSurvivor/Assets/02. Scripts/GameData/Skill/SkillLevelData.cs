using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SkillLevelData
{
    [SerializeField] private int level;
    [TextArea]
    [SerializeField] private string description;

    [Header("Combat Values")]
    [SerializeField] private float damage;
    [SerializeField] private float cooldown;
    [SerializeField] private float range;
    [SerializeField] private float duration;
    [SerializeField] private float size;
    [SerializeField] private float speed;
    [SerializeField] private int projectileCount;
    [SerializeField] private int pierceCount;

    [Header("Additional Stat Modifiers")]
    [SerializeField] private List<StatModifierData> statModifiers = new();

    public int Level => level;
    public string Description => description;

    public float Damage => damage;
    public float Cooldown => cooldown;
    public float Range => range;
    public float Duration => duration;
    public float Size => size;
    public float Speed => speed;
    public int ProjectileCount => projectileCount;
    public int PierceCount => pierceCount;
    //나중에 필요하면 SkillLevelData에 tickInterval, summonCount, attackRate 같은 값을 추가
    public IReadOnlyList<StatModifierData> StatModifiers => statModifiers;
}