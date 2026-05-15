using System;
using UnityEngine;

[Serializable]
public class StatModifierData
{
    [SerializeField] private EStatType statType;
    [SerializeField] private float value;
    [SerializeField] private bool isPercent;

    public EStatType StatType => statType;
    public float Value => value;
    public bool IsPercent => isPercent;
}