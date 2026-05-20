using UnityEngine;

public class WeaponBase : MonoBehaviour
{
    protected Transform Owner { get; private set; }
    protected Transform OwnerFirePoint { get; private set; }
    protected WeaponInstance RuntimeData { get; private set; }

    protected float attackTimer;

    public void Initialize(Transform owner, WeaponInstance runtimeData)
    {
        Initialize(owner, runtimeData, null);
    }

    public void Initialize(Transform owner, WeaponInstance runtimeData, Transform ownerFirePoint)
    {
        if (RuntimeData != null)
        {
            RuntimeData.LevelChanged -= HandleRuntimeLevelChanged;
        }

        Owner = owner;
        OwnerFirePoint = ownerFirePoint;
        RuntimeData = runtimeData;
        attackTimer = 0f;

        if (RuntimeData != null)
        {
            RuntimeData.LevelChanged += HandleRuntimeLevelChanged;
            weaponID = RuntimeData.Definition.WeaponId;
            weaponName = RuntimeData.Definition.WeaponName;
        }

        OnInitialized();
        OnLevelDataChanged(GetCurrentLevelData());
    }

    protected virtual void OnInitialized()
    {
    }

    protected virtual void Update()
    {
        WeaponLevelData data = GetCurrentLevelData();
        if (data == null)
            return;

        attackTimer += Time.deltaTime;

        if (attackTimer < GetFinalCooldown(data.Cooldown))
            return;

        attackTimer = 0f;
        Attack(data);
    }

    protected virtual void Attack(WeaponLevelData data)
    {
    }

    protected virtual void OnLevelDataChanged(WeaponLevelData data)
    {
    }

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

    protected bool HasConfiguredLevelData()
    {
        return RuntimeData != null || (levelData != null && levelData.Length > 0);
    }

    protected Transform ResolveFirePoint(Transform configuredFirePoint)
    {
        if (configuredFirePoint != null)
            return configuredFirePoint;

        if (OwnerFirePoint != null)
            return OwnerFirePoint;

        return Owner != null ? Owner : transform;
    }

    public string GetWeaponID()
    {
        return weaponID;
    }

    public string GetWeaponName()
    {
        return weaponName;
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

    private void HandleRuntimeLevelChanged(int _)
    {
        OnLevelDataChanged(GetCurrentLevelData());
    }

    protected virtual void OnDestroy()
    {
        if (RuntimeData != null)
        {
            RuntimeData.LevelChanged -= HandleRuntimeLevelChanged;
        }
    }
}
