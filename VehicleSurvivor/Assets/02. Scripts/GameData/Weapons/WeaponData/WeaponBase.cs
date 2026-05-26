using UnityEngine;

public class WeaponBase : MonoBehaviour
{
    protected Transform Owner { get; private set; }
    protected Transform OwnerFirePoint { get; private set; }
    protected WeaponInstance RuntimeData { get; private set; }

    private PlayerRuntime ownerRuntime;

#if UNITY_EDITOR
    [Header("Editor Test Override")]
    [SerializeField] private bool useEditorTestLevelData;
    [SerializeField] private WeaponLevelData editorTestLevelData = new();

    private float editorTestCooldown;
#endif

    public void Initialize(Transform owner, WeaponInstance runtimeData)
    {
        Initialize(owner, runtimeData, null, null);
    }

    public void Initialize(Transform owner, WeaponInstance runtimeData, Transform ownerFirePoint)
    {
        Initialize(owner, runtimeData, ownerFirePoint, null);
    }

    public void Initialize(
        Transform owner,
        WeaponInstance runtimeData,
        Transform ownerFirePoint,
        PlayerRuntime ownerRuntime)
    {
        if (RuntimeData != null)
        {
            RuntimeData.LevelChanged -= HandleRuntimeLevelChanged;
        }

        Owner = owner;
        OwnerFirePoint = ownerFirePoint;
        RuntimeData = runtimeData;
        this.ownerRuntime = ownerRuntime;

#if UNITY_EDITOR
        editorTestCooldown = 0f;
#endif

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

#if UNITY_EDITOR
        if (IsUsingEditorTestLevelData())
        {
            TickEditorTestWeapon(data);
            return;
        }
#endif

        if (RuntimeData == null)
            return;

        RuntimeData.TickCooldown(Time.deltaTime);

        if (!RuntimeData.IsReady)
            return;

        Attack(data);
        SoundPlayback.PlaySfx(ESoundId.SFX_Weapon_Fire);
        RuntimeData.ResetCooldown(GetFinalCooldown(data.Cooldown));
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

    protected virtual void Awake()
    {
    }

    protected WeaponLevelData GetCurrentLevelData()
    {
#if UNITY_EDITOR
        if (IsUsingEditorTestLevelData())
            return editorTestLevelData;
#endif

        return RuntimeData?.CurrentLevelData;
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

    protected DamageInfo GetFinalDamage(float baseDamage)
    {
        PlayerRuntime playerRuntime = GetOwnerRuntime();
        if (playerRuntime == null)
            return DamageInfo.Normal(baseDamage);

        RuntimeStats stats = playerRuntime.Stats;
        float damage = baseDamage * stats.GetValue(EStatType.DamageMultiplier);

        float criticalChance = stats.GetValue(EStatType.CriticalChance);
        bool isCritical = Random.value < criticalChance;
        if (isCritical)
            damage *= stats.GetValue(EStatType.CriticalDamageMultiplier);

        return new DamageInfo(damage, isCritical);
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

    protected float GetFinalWeaponScale(float baseScale)
    {
        PlayerRuntime playerRuntime = GetOwnerRuntime();
        if (playerRuntime == null)
            return baseScale;

        return baseScale * playerRuntime.Stats.GetValue(EStatType.WeaponSizeMultiplier);
    }

    private PlayerRuntime GetOwnerRuntime()
    {
        if (ownerRuntime != null)
            return ownerRuntime;

        if (!RunManager.TryGetInstance(out RunManager runManager))
            return null;

        ownerRuntime = runManager.RunSession?.Player;
        return ownerRuntime;
    }

    private void HandleRuntimeLevelChanged(int _)
    {
        OnLevelDataChanged(GetCurrentLevelData());
    }

#if UNITY_EDITOR
    private bool IsUsingEditorTestLevelData()
    {
        return useEditorTestLevelData && editorTestLevelData != null;
    }

    private void TickEditorTestWeapon(WeaponLevelData data)
    {
        editorTestCooldown -= Time.deltaTime;

        if (editorTestCooldown > 0f)
            return;

        Attack(data);
        editorTestCooldown = GetFinalCooldown(data.Cooldown);
    }

    protected virtual void OnValidate()
    {
        if (!Application.isPlaying || !IsUsingEditorTestLevelData())
            return;

        OnLevelDataChanged(editorTestLevelData);
    }
#endif

    protected virtual void OnDestroy()
    {
        if (RuntimeData != null)
        {
            RuntimeData.LevelChanged -= HandleRuntimeLevelChanged;
        }
    }
}
