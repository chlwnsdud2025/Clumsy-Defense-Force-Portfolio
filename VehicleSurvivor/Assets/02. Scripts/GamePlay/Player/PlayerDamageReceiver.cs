using UnityEngine;

public class PlayerDamageReceiver : MonoBehaviour, IDamageable
{
    public float ContactDamage => 1f;

    [SerializeField] private HitFlashEffect hitFlash;
    [SerializeField] private PlayerHitCameraShake cameraShake;

    private PlayerRuntime runtime;

    private HitFlashEffect HitFlash
    {
        get
        {
            if (hitFlash == null)
                hitFlash = GetComponentInParent<HitFlashEffect>();
            return hitFlash;
        }
    }

    private PlayerHitCameraShake CameraShake
    {
        get
        {
            if (cameraShake == null)
                cameraShake = GetComponentInParent<PlayerHitCameraShake>();
            return cameraShake;
        }
    }

    public void BindRuntime(PlayerRuntime runtime)
    {
        this.runtime = runtime;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (!TryGetPlayerRuntime(out PlayerRuntime playerRuntime))
            return;

        if (playerRuntime.Health.CurrentHp <= 0f)
            return;

        playerRuntime.Health.TakeDamage(damageInfo.Amount);
        SoundPlayback.PlaySfx(ESoundId.SFX_Player_Hurt);

        HitFlash?.TriggerHitFlash();
        CameraShake?.TriggerShake();
    }

    public void Heal(float amount)
    {
        if (!TryGetPlayerRuntime(out PlayerRuntime playerRuntime))
            return;

        if (playerRuntime.Health.CurrentHp <= 0f)
            return;

        playerRuntime.Health.Heal(amount);
        SoundPlayback.PlaySfx(ESoundId.SFX_Player_Heal);
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
