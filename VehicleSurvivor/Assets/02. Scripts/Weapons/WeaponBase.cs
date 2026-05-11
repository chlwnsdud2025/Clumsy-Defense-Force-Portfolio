using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("무기 레벨 데이터")]
    public WeaponLevelData[] levelData;

    [Header("현재 무기 상태")]
    [SerializeField] protected int currentLevel = 0;
    protected float attackTimer;

    protected virtual void Update()
    {
        attackTimer += Time.deltaTime;

        WeaponLevelData data = GetCurrentLevelData();

        if (data == null)
            return;

        if (attackTimer >= data.cooldown)
        {
            Attack();
            attackTimer = 0f;
        }
    }

    public void LevelUpWeapon()
    {
        if (levelData == null || levelData.Length == 0)
            return;

        currentLevel++;

        if (currentLevel >= levelData.Length)
        {
            currentLevel = levelData.Length - 1;
        }

        OnWeaponLevelUp();
    }

    protected WeaponLevelData GetCurrentLevelData()
    {
        if (levelData == null || levelData.Length == 0)
            return null;

        return levelData[currentLevel];
    }

    protected virtual void OnWeaponLevelUp()
    {
        Debug.Log($"{gameObject.name} 무기 레벨업! 현재 레벨: {currentLevel + 1}");
    }

    protected abstract void Attack();
}