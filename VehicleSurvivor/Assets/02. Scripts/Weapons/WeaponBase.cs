using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Info")]
    public string weaponID;
    public string weaponName;

    [Header("Weapon Stats")]
    public int level = 1;
    public int maxLevel = 5;
    public float damage = 10f;
    public float cooldown = 1f;

    protected float attackTimer;

    protected virtual void Update()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer >= cooldown)
        {
            Attack();
            attackTimer = 0f;
        }
    }

    protected abstract void Attack();

    public virtual void LevelUp()
    {
        if (level >= maxLevel)
        {
            Debug.Log($"{weaponName}은 이미 최대 레벨입니다.");
            return;
        }

        level++;

        damage += 5f;
        cooldown *= 0.9f;

        Debug.Log($"{weaponName} 레벨업! 현재 레벨: {level}");
    }
}       