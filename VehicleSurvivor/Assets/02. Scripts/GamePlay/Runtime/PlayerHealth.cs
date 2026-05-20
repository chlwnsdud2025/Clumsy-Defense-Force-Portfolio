using System;
using UnityEngine;

public sealed class PlayerHealth
{
    //현재 체력 관리
    //피해 처리
    //회복 처리
    //최대 체력 변경 시 현재 체력 보정
    //사망 이벤트 발생

    private readonly RuntimeStats stats;

    public float CurrentHp { get; private set; }
    public float MaxHp => stats.GetValue(EStatType.MaxHealth);

    public event Action<float, float> OnHpChanged;
    public event Action OnDied;

    public PlayerHealth(RuntimeStats stats)
    {
        this.stats = stats;

        CurrentHp = MaxHp;

        stats.OnStatChanged += HandleStatChanged;
    }

    public void TakeDamage(float damage)
    {
        float defense = stats.GetValue(EStatType.Defense);
        float finalDamage = Mathf.Max(1f, damage - defense);

        CurrentHp = Mathf.Max(0f, CurrentHp - finalDamage);

        OnHpChanged?.Invoke(CurrentHp, MaxHp);

        if (CurrentHp <= 0f)
        {
            OnDied?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        CurrentHp = Mathf.Min(CurrentHp + amount, MaxHp);

        OnHpChanged?.Invoke(CurrentHp, MaxHp);
    }

    private void HandleStatChanged(EStatType statType, float oldValue, float newValue)
    {
        if (statType != EStatType.MaxHealth)
            return;

        float increasedAmount = newValue - oldValue;

        if (increasedAmount > 0f)
        {
            CurrentHp += increasedAmount;
        }

        CurrentHp = Mathf.Clamp(CurrentHp, 0f, newValue);

        OnHpChanged?.Invoke(CurrentHp, newValue);
    }
}