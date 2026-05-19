using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    protected Transform Owner { get; private set; }
    protected WeaponRuntimeData RuntimeData { get; private set; }

    private float attackTimer;

    public void Initialize(Transform owner, WeaponRuntimeData runtimeData)
    {
        Owner = owner;
        RuntimeData = runtimeData;
        attackTimer = 0f;

        OnInitialized();
    }

    protected virtual void OnInitialized()
    {
    }

    protected virtual void Update()
    {
        if (RuntimeData == null)
            return;

        WeaponLevelData data = RuntimeData.CurrentLevelData;
        if (data == null)
            return;

        attackTimer += Time.deltaTime;

        if (attackTimer < GetFinalCooldown(data.Cooldown))
            return;

        attackTimer = 0f;
        Attack(data);
    }

    protected abstract void Attack(WeaponLevelData data);

    [Header("Weapon Info")]
    public string weaponID = "weapon";
    public string weaponName = "Weapon";

    [Header("Legacy Level Data")]
    public WeaponLevelData[] levelData;

    [Header("Legacy State")]
    [SerializeField] protected int currentLevel = 0;

    protected virtual void Awake()
    {
    }

    protected WeaponLevelData GetCurrentLevelData()
    {
        if (RuntimeData != null)
            return RuntimeData.CurrentLevelData;

        if (levelData == null || levelData.Length == 0)
            return null;

        currentLevel = Mathf.Clamp(currentLevel, 0, levelData.Length - 1);
        return levelData[currentLevel];
    }

    public void LevelUpWeapon()
    {
        if (RuntimeData != null)
        {
            if (RuntimeData.TryLevelUp())
                OnWeaponLevelUp();

            return;
        }

        if (levelData == null || levelData.Length == 0)
            return;

        if (currentLevel < levelData.Length - 1)
        {
            currentLevel++;
            OnWeaponLevelUp();
        }
    }

    protected virtual void OnWeaponLevelUp()
    {
        int level = RuntimeData != null ? RuntimeData.CurrentLevel : currentLevel + 1;
        Debug.Log($"{weaponName} level up: {level}");
    }

    protected float GetFinalDamage(float baseDamage)
    {
        PlayerRuntime playerRuntime = GetOwnerRuntime();
        if (playerRuntime == null)
            return baseDamage;

        return baseDamage * playerRuntime.Stats.GetValue(EStatType.DamageMultiplier);
    }

    protected float GetFinalCooldown(float weaponCooldown)
    {
        PlayerRuntime playerRuntime = GetOwnerRuntime();
        if (playerRuntime == null)
            return weaponCooldown;

        float cooldownReduction = playerRuntime.Stats.GetValue(EStatType.CooldownReduction);
        return Mathf.Max(0.01f, weaponCooldown * Mathf.Max(0f, 1f - cooldownReduction));
    }

    protected int GetFinalProjectileCount(int weaponProjectileCount)
    {
        PlayerRuntime playerRuntime = GetOwnerRuntime();
        if (playerRuntime == null)
            return weaponProjectileCount;

        int projectileBonus = Mathf.RoundToInt(playerRuntime.Stats.GetValue(EStatType.ProjectileCount));
        return weaponProjectileCount + projectileBonus;
    }

    private static PlayerRuntime GetOwnerRuntime()
    {
        if (!RunManager.TryGetInstance(out RunManager runManager))
            return null;

        return runManager.RunContext?.Player;
    }
}
