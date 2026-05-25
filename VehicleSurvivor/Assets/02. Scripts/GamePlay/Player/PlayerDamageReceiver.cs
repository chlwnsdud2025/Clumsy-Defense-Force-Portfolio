using UnityEngine;

public class PlayerDamageReceiver : MonoBehaviour, IDamageable
{
    public float ContactDamage => 1f;

    private PlayerRuntime runtime;
    private HitFlashEffect hitFlash;

    private void Awake()
    {
        PlayerVisual visual = GetComponentInParent<PlayerVisual>();
        if (visual != null)
            hitFlash = visual.HitFlash;
    }

    public void BindRuntime(PlayerRuntime runtime)
    {
        this.runtime = runtime;
    }

    public void TakeDamage(float damage)
    {
        if (!TryGetPlayerRuntime(out PlayerRuntime playerRuntime))
            return;

        if (playerRuntime.Health.CurrentHp <= 0f)
            return;

        float previousHp = playerRuntime.Health.CurrentHp;
        playerRuntime.Health.TakeDamage(damage);

        if (playerRuntime.Health.CurrentHp < previousHp)
            hitFlash?.TriggerHitFlash();
    }

    public void Heal(float amount)
    {
        if (!TryGetPlayerRuntime(out PlayerRuntime playerRuntime))
            return;

        if (playerRuntime.Health.CurrentHp <= 0f)
            return;

        playerRuntime.Health.Heal(amount);
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
