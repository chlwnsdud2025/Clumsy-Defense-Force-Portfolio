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
    private float regenAccumulator;

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
        // 비율 감소: 방어력 100 = 피해 50%. 고정 차감과 달리 방어력을 쌓아도 무적이 되지 않는다.
        float defense = Mathf.Max(0f, stats.GetValue(EStatType.Defense));
        float finalDamage = Mathf.Max(1f, damage * 100f / (100f + defense));

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

    public void TickRegen(float deltaTime)
    {
        float regenPerSecond = stats.GetValue(EStatType.HealthRegen);
        if (regenPerSecond <= 0f || CurrentHp >= MaxHp)
            return;

        regenAccumulator += regenPerSecond * deltaTime;

        if (regenAccumulator < 1f)
            return;

        float healAmount = Mathf.Floor(regenAccumulator);
        regenAccumulator -= healAmount;
        Heal(healAmount);
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