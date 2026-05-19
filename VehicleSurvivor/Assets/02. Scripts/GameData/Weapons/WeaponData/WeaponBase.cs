using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("무기 데이터")]
    public WeaponDataBaseSO weaponData;

    [Header("현재 무기 상태")]
    [SerializeField]
    protected int currentLevel = 0;

    protected PlayerStats playerStats;

    protected virtual void Awake()
    {
        playerStats = GetComponentInParent<PlayerStats>();

        if (weaponData == null)
        {
            Debug.LogWarning($"{gameObject.name} WeaponData가 비어있습니다.");
        }
    }

    public virtual void LevelUpWeapon()
    {
        currentLevel++;

        Debug.Log($"{GetWeaponName()} 레벨업! 현재 레벨: {currentLevel + 1}");
    }

    public string GetWeaponID()
    {
        if (weaponData == null)
            return gameObject.name;

        return weaponData.weaponID;
    }

    public string GetWeaponName()
    {
        if (weaponData == null)
            return gameObject.name;

        return weaponData.weaponName;
    }

    protected float GetFinalDamage(float baseDamage)
    {
        if (playerStats == null)
            return baseDamage;

        return playerStats.GetFinalDamage(baseDamage);
    }

    protected float GetFinalCooldown(float baseCooldown)
    {
        if (playerStats == null)
            return baseCooldown;

        return playerStats.GetFinalCooldown(baseCooldown);
    }

    protected int GetFinalProjectileCount(int projectileCount)
    {
        if (playerStats == null)
            return projectileCount;

        return playerStats.GetFinalProjectileCount(projectileCount);
    }
}