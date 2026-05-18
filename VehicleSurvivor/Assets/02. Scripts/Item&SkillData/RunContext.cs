using System.Collections.Generic;
using UnityEngine;

public sealed class RunContext
{
    public CharacterDefinitionSO Character { get; }

    public RuntimeStats Stats { get; private set; }

    public int CurrentHp { get; private set; }
    public int MaxHp => Mathf.RoundToInt(Stats.MaxHealth);

    private readonly Dictionary<string, int> _passiveLevels = new();
    private readonly HashSet<string> _ownedItems = new();

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
    public int GetPassiveLevel(string passiveId)
    {
        if(string.IsNullOrWhiteSpace(passiveId))
            return 0;
        return _passiveLevels.TryGetValue(passiveId, out int level) ? level : 0;
    }
   
    public void AddPassiveLevel(string passiveId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(passiveId))
            return;
        int currentLevel = GetPassiveLevel(passiveId);
        _passiveLevels[passiveId] = Mathf.Max(0, currentLevel + amount);
        //_passiveLevels[passiveId] = level;
    }
    public void SetPassiveLevel(string passiveId, int level)
    {
        if (string.IsNullOrWhiteSpace(passiveId))
            return;

        _passiveLevels[passiveId] = Mathf.Max(0, level);
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