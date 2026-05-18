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

    protected virtual void Update() // TODO : private void Update()?? 로 바꾸는게 더 나은지?

    {
        if (RuntimeData == null)
            return;

        WeaponLevelData data = RuntimeData.CurrentLevelData;

        if (data == null)
            return;

        attackTimer += Time.deltaTime;

        if (attackTimer < data.Cooldown)
            return;

        attackTimer = 0f;

        Attack(data);
    }
    protected abstract void Attack(WeaponLevelData data);


    //<기존 코드> - TODO : 나중에 병합과정에서 수정필요

    [Header("무기 정보")]
    public string weaponID = "weapon";
    public string weaponName = "Weapon";

    [Header("레벨 데이터")]
    public WeaponLevelData[] levelData;

    [Header("현재 상태")]
    [SerializeField] protected int currentLevel = 0;

    //protected float attackTimer;
    protected PlayerStats playerStats;

    protected virtual void Awake()
    {
        playerStats = GetComponentInParent<PlayerStats>();
    }

    //protected virtual void Update()
    //{
    //    WeaponLevelData data = GetCurrentLevelData();

    //    if (data == null)
    //        return;

    //    attackTimer += Time.deltaTime;

    //    if (attackTimer >= data.Cooldown)
    //    {
    //        Attack(data);
    //        attackTimer = 0f;
    //    }
    //}

    protected WeaponLevelData GetCurrentLevelData()
    {
        if (levelData == null || levelData.Length == 0)
            return null;

        currentLevel = Mathf.Clamp(currentLevel, 0, levelData.Length - 1);
        return levelData[currentLevel];
    }

    public void LevelUpWeapon()
    {
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
        Debug.Log($"{weaponName} 레벨업! 현재 레벨: {currentLevel + 1}");
    }

    protected float GetFinalDamage(float baseDamage)
    {
        if (playerStats == null)
            return baseDamage;

        return playerStats.GetFinalDamage(baseDamage);
    }

    //protected abstract void Attack(WeaponLevelData data);
}