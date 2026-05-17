using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
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

    [Header("Knockback")]
    public float knockbackPower = 5f;
    public float knockbackDuration = 0.15f;
    public float ContactDamage => 1;


    private Vector3 knockbackVelocity;
    private float knockbackTimer;

    public bool IsKnockback => knockbackTimer > 0f;

    private void Update()
    {
        if (knockbackTimer > 0f)
        {
            transform.position += knockbackVelocity * Time.deltaTime;
            knockbackTimer -= Time.deltaTime;
        }
    }

    private void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        Debug.Log("Player 데미지 입음:" +  damage);
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

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;
        if (knockbackTimer > 0f) return;

        // 잡몹이든 보스든 IDamageable 인터페이스로 한 번에 가져옵니다.
        IDamageable damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();

        if (damageable == null) return;

        // 인터페이스를 통해 상대방의 접촉 데미지를 가져옵니다.
        float damage = damageable.ContactDamage;

        TakeDamage(damage);

        // 밀려날 방향 계산 (상대방의 Transform 위치 기반)
        Component enemyComponent = (Component)damageable; // 위치 값을 알기 위해 Component로 캐스팅
        Vector3 pushDir = transform.position - enemyComponent.transform.position;
        pushDir.y = 0f;

        if (pushDir == Vector3.zero) pushDir = -transform.forward;

        knockbackVelocity = pushDir.normalized * knockbackPower;
        knockbackTimer = knockbackDuration;

        Debug.Log($"플레이어 피격! 데미지: {damage}");
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