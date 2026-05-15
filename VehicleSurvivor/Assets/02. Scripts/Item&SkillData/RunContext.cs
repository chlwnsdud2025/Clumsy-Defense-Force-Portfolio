using System.Collections.Generic;
using UnityEngine;

public sealed class RunContext
{
    public CharacterDefinitionSO Character { get; }

    public RuntimeStats Stats { get; private set; }

    public int CurrentHp { get; private set; }
    public int MaxHp => Mathf.RoundToInt(Stats.MaxHealth);

    private readonly Dictionary<string, int> _skillLevels = new();
    private readonly Dictionary<string, int> _passiveLevels = new();
    private readonly HashSet<string> _ownedItems = new();

    public IReadOnlyDictionary<string, int> SkillLevels => _skillLevels;
    public IReadOnlyDictionary<string, int> PassiveLevels => _passiveLevels;
    public IReadOnlyCollection<string> OwnedItems => _ownedItems;

    public RunContext(CharacterDefinitionSO character)
    {
        Character = character;
        Stats = RuntimeStats.FromCharacter(character);
        CurrentHp = MaxHp;
    }

    public bool HasPassive(string passiveId)
    {
        return !string.IsNullOrWhiteSpace(passiveId)
            && _passiveLevels.ContainsKey(passiveId);
    }

    public int GetSkillLevel(string skillId)
    {
        return _skillLevels.TryGetValue(skillId, out int level) ? level : 0;
    }

    public void SetSkillLevel(string skillId, int level)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return;

        _skillLevels[skillId] = Mathf.Max(0, level);
    }

    public void AddPassiveLevel(string passiveId, int level)
    {
        if (string.IsNullOrWhiteSpace(passiveId))
            return;

        _passiveLevels[passiveId] = level;
    }

    public bool AddOwnedItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return false;

        return _ownedItems.Add(itemId);
    }

    public void ApplyStats(RuntimeStats stats)
    {
        Stats = stats;

        if (CurrentHp > MaxHp)
            CurrentHp = MaxHp;
    }

    public void Heal(int amount)
    {
        CurrentHp = Mathf.Min(CurrentHp + amount, MaxHp);
    }

    public void TakeDamage(int amount)
    {
        CurrentHp = Mathf.Max(0, CurrentHp - amount);
    }
}