using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("HP")]
    public float maxHP = 100f;
    public float currentHP;

    [Header("Attack")]
    public float attackPowerMultiplier = 1f;   // 전체 무기 데미지 배율
    public float attackSpeedMultiplier = 1f;   // 전체 무기 공격속도 배율
    public int bonusProjectileCount = 0;       // 추가 탄환 수

    [Header("Move")]
    public float moveSpeedMultiplier = 1f;
    public float boostPowerMultiplier = 1f;

    [Header("Level")]
    public int level = 1;
    public int currentExp = 0;
    public int needExp = 10;

    public bool isDead;

    private void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        currentHP += amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
    }

    public void AddExp(int expAmount)
    {
        if (isDead) return;

        currentExp += expAmount;

        while (currentExp >= needExp)
        {
            currentExp -= needExp;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        level++;

        needExp += 5;

        Debug.Log($"레벨업! 현재 레벨: {level}");
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("플레이어 사망");
    }

    public float GetFinalDamage(float weaponDamage)
    {
        return weaponDamage * attackPowerMultiplier;
    }

    public float GetFinalCooldown(float weaponCooldown)
    {
        return weaponCooldown / attackSpeedMultiplier;
    }

    public int GetFinalProjectileCount(int weaponProjectileCount)
    {
        return weaponProjectileCount + bonusProjectileCount;
    }
}