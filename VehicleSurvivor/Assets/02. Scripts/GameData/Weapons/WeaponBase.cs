using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("무기 데이터")]
    public WeaponDataSO weaponData;

    [Header("현재 무기 상태")]
    [SerializeField]
    protected int currentLevel = 0;

    protected float attackTimer;
    protected PlayerStats playerStats;

    protected virtual void Awake()
    {
        playerStats =
            GetComponentInParent<PlayerStats>();

        if (weaponData == null)
        {
            Debug.LogWarning(
                $"{gameObject.name} WeaponDataSO가 비어있습니다."
            );
        }
    }

    protected virtual void Update()
    {
        WeaponLevelData data =
            GetCurrentLevelData();

        if (data == null)
            return;

        float finalCooldown =
            GetFinalCooldown(
                data.cooldown
            );

        attackTimer +=
            Time.deltaTime;

        if (
            attackTimer
            >= finalCooldown
        )
        {
            Attack(data);

            attackTimer = 0f;
        }
    }

    protected WeaponLevelData
        GetCurrentLevelData()
    {
        if (weaponData == null)
            return null;

        if (
            weaponData.levelData
            == null
            || weaponData
                .levelData
                .Length
                == 0
        )
        {
            return null;
        }

        currentLevel =
            Mathf.Clamp(
                currentLevel,
                0,
                weaponData
                .levelData
                .Length - 1
            );

        return weaponData
            .levelData[
                currentLevel
            ];
    }

    public void LevelUpWeapon()
    {
        if (
            weaponData == null
        )
            return;

        int maxLevel =
            weaponData
            .levelData
            .Length - 1;

        if (
            currentLevel
            < maxLevel
        )
        {
            currentLevel++;

            Debug.Log(
                $"{GetWeaponName()} 레벨업! 현재 레벨: {currentLevel + 1}"
            );
        }
        else
        {
            Debug.Log(
                $"{GetWeaponName()} 최대 레벨"
            );
        }
    }

    public string GetWeaponID()
    {
        if (
            weaponData == null
        )
            return gameObject.name;

        return weaponData
            .weaponID;
    }

    public string GetWeaponName()
    {
        if (
            weaponData == null
        )
            return gameObject.name;

        return weaponData
            .weaponName;
    }

    protected float GetFinalDamage(
        float baseDamage
    )
    {
        if (
            playerStats
            == null
        )
        {
            return baseDamage;
        }

        return playerStats
            .GetFinalDamage(
                baseDamage
            );
    }

    protected float GetFinalCooldown(
        float baseCooldown
    )
    {
        if (
            playerStats
            == null
        )
        {
            return baseCooldown;
        }

        return playerStats
            .GetFinalCooldown(
                baseCooldown
            );
    }

    protected int
        GetFinalProjectileCount(
            int projectileCount
        )
    {
        if (
            playerStats
            == null
        )
        {
            return projectileCount;
        }

        return playerStats
            .GetFinalProjectileCount(
                projectileCount
            );
    }

    protected abstract void Attack(
        WeaponLevelData data
    );
}