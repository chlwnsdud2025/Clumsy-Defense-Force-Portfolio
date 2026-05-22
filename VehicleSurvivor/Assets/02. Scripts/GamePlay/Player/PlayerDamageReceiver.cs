using UnityEngine;

public class PlayerDamageReceiver : MonoBehaviour, IDamageable
{
    [Header("Knockback")]
    public float knockbackPower = 5f;
    public float knockbackDuration = 0.15f;
    public float ContactDamage => 1f;

    private Vector3 knockbackVelocity;
    private float knockbackTimer;
    private PlayerRuntime runtime;

    public bool IsKnockback => knockbackTimer > 0f;

    public void BindRuntime(PlayerRuntime runtime)
    {
        this.runtime = runtime;
    }

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

    private bool TryGetPlayerRuntime(out PlayerRuntime playerRuntime)
    {
        playerRuntime = runtime;
        if (playerRuntime != null)
            return true;

        if (!RunManager.TryGetInstance(out RunManager runManager))
            return false;

        RunSession runSession = runManager.RunSession;
        if (runSession == null)
            return false;

        playerRuntime = runSession.Player;
        return playerRuntime != null;
    }
}
