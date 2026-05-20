using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Knockback")]
    public float knockbackPower = 5f;
    public float knockbackDuration = 0.15f;
    public float ContactDamage => 1f;

    private Vector3 knockbackVelocity;
    private float knockbackTimer;

    public bool IsKnockback => knockbackTimer > 0f;

    private void Update()
    {
        if (knockbackTimer <= 0f)
            return;

        transform.position += knockbackVelocity * Time.deltaTime;
        knockbackTimer -= Time.deltaTime;
    }

    public void TakeDamage(float damage)
    {
        if (!TryGetPlayerRuntime(out PlayerRuntime playerRuntime))
            return;

        if (playerRuntime.Health.CurrentHp <= 0f)
            return;

        playerRuntime.Health.TakeDamage(damage);
    }

    public void Heal(float amount)
    {
        if (!TryGetPlayerRuntime(out PlayerRuntime playerRuntime))
            return;

        if (playerRuntime.Health.CurrentHp <= 0f)
            return;

        playerRuntime.Health.Heal(amount);
    }

    public void AddExp(int expAmount)
    {
        if (expAmount <= 0)
            return;

        if (!TryGetPlayerRuntime(out PlayerRuntime playerRuntime))
            return;

        if (playerRuntime.Health.CurrentHp <= 0f)
            return;

        playerRuntime.Level.AddExp(expAmount);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        if (knockbackTimer > 0f)
            return;

        IDamageable damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return;

        float damage = damageable.ContactDamage;
        TakeDamage(damage);

        Component enemyComponent = damageable as Component;
        if (enemyComponent == null)
            return;

        Vector3 pushDir = transform.position - enemyComponent.transform.position;
        pushDir.y = 0f;

        if (pushDir == Vector3.zero)
            pushDir = -transform.forward;

        knockbackVelocity = pushDir.normalized * knockbackPower;
        knockbackTimer = knockbackDuration;
    }

    public float GetFinalDamage(float weaponDamage)
    {
        if (!TryGetPlayerRuntime(out PlayerRuntime playerRuntime))
            return weaponDamage;

        float damageMultiplier = playerRuntime.Stats.GetValue(EStatType.DamageMultiplier);
        return weaponDamage * damageMultiplier;
    }

    public float GetFinalCooldown(float weaponCooldown)
    {
        if (!TryGetPlayerRuntime(out PlayerRuntime playerRuntime))
            return weaponCooldown;

        float cooldownReduction = playerRuntime.Stats.GetValue(EStatType.CooldownReduction);
        return Mathf.Max(0.01f, weaponCooldown * Mathf.Max(0f, 1f - cooldownReduction));
    }

    public int GetFinalProjectileCount(int weaponProjectileCount)
    {
        if (!TryGetPlayerRuntime(out PlayerRuntime playerRuntime))
            return weaponProjectileCount;

        int projectileBonus = Mathf.RoundToInt(playerRuntime.Stats.GetValue(EStatType.ProjectileCount));
        return weaponProjectileCount + projectileBonus;
    }

    private static bool TryGetPlayerRuntime(out PlayerRuntime playerRuntime)
    {
        playerRuntime = null;

        if (!RunManager.TryGetInstance(out RunManager runManager))
            return false;

        RunContext runContext = runManager.RunContext;
        if (runContext == null)
            return false;

        playerRuntime = runContext.Player;
        return playerRuntime != null;
    }
}
