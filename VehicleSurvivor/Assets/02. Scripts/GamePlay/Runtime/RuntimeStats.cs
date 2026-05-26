using System;
using System.Collections.Generic;

public enum EStatModifierType
{
    Flat,
    Percent
}

[System.Serializable]
public sealed class RuntimeStats //PlayerRuntime 안에 들어있는 스탯 계산기
{
    private readonly Dictionary<EStatType, float> baseStats = new();
    private readonly List<RuntimeModifier> modifiers = new();

    public event Action<EStatType, float, float> OnStatChanged;

    public void SetBaseStat(EStatType statType, float value)
    {
        float oldValue = GetValue(statType);

        baseStats[statType] = value;

        float newValue = GetValue(statType);
        if (oldValue != newValue)
            OnStatChanged?.Invoke(statType, oldValue, newValue);
    }

    public float GetValue(EStatType statType)
    {
        float value = baseStats.TryGetValue(statType, out float baseValue)
            ? baseValue
            : 0f;
        float flatBonus = 0f;
        float percentBonus = 0f;

        foreach (RuntimeModifier modifier in modifiers)
        {
            if (modifier.StatType != statType)
                continue;

            if (modifier.ModifierType == EStatModifierType.Flat)
            {
                flatBonus += modifier.Value;
            }
            else if (modifier.ModifierType == EStatModifierType.Percent)
            {
                percentBonus += modifier.Value;
            }
        }

        value += flatBonus;

        if (value <= 0f && percentBonus != 0f)
            return percentBonus;

        value *= 1f + percentBonus;
        return value;
    }

    public void AddModifier(
        object source,
        EStatType statType,
        EStatModifierType modifierType,
        float value)
    {
        float oldValue = GetValue(statType);

        modifiers.Add(new RuntimeModifier(source, statType, modifierType, value));

        float newValue = GetValue(statType);
        if (oldValue != newValue)
            OnStatChanged?.Invoke(statType, oldValue, newValue);
    }

    public void RemoveModifiersFrom(object source)
    {
        List<EStatType> changedStatTypes = new();
        Dictionary<EStatType, float> oldValues = new();

        foreach (RuntimeModifier modifier in modifiers)
        {
            if (modifier.Source != source)
                continue;

            if (!changedStatTypes.Contains(modifier.StatType))
            {
                changedStatTypes.Add(modifier.StatType);
                oldValues[modifier.StatType] = GetValue(modifier.StatType);
            }
        }

        modifiers.RemoveAll(modifier => modifier.Source == source);

        foreach (EStatType statType in changedStatTypes)
        {
            float oldValue = oldValues[statType];
            float newValue = GetValue(statType);

            if (oldValue != newValue)
                OnStatChanged?.Invoke(statType, oldValue, newValue);
        }
    }

    public void ReplaceModifier(
        object source,
        EStatType statType,
        EStatModifierType modifierType,
        float value)
    {
        RemoveModifiersFrom(source);
        AddModifier(source, statType, modifierType, value);
    }

    private sealed class RuntimeModifier
    {
        public object Source { get; }
        public EStatType StatType { get; }
        public EStatModifierType ModifierType { get; }
        public float Value { get; }

        public RuntimeModifier(
            object source,
            EStatType statType,
            EStatModifierType modifierType,
            float value)
        {
            Source = source;
            StatType = statType;
            ModifierType = modifierType;
            Value = value;
        }
    }
}
