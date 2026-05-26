using System.Collections.Generic;
using UnityEngine;

public class OrbitObject : MonoBehaviour
{
    private DamageInfo damageInfo;
    private float hitCooldown;

    private readonly Dictionary<IDamageable, float> hitTimers =
        new Dictionary<IDamageable, float>();

    public void Initialize(
        DamageInfo newDamageInfo,
        float newHitCooldown
    )
    {
        damageInfo = newDamageInfo;
        hitCooldown = Mathf.Max(0.05f, newHitCooldown);
    }

    private void OnTriggerStay(Collider other)
    {
        IDamageable damageable =
            other.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        if (hitTimers.ContainsKey(damageable))
        {
            if (Time.time < hitTimers[damageable])
                return;

            hitTimers[damageable] =
                Time.time + hitCooldown;
        }
        else
        {
            hitTimers.Add(
                damageable,
                Time.time + hitCooldown
            );
        }

        damageable.TakeDamage(damageInfo);

        Debug.Log($"[OrbitObject] 데미지: {damageInfo.Amount}");
    }

    private void OnTriggerExit(Collider other)
    {
        IDamageable damageable =
            other.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        if (hitTimers.ContainsKey(damageable))
        {
            hitTimers.Remove(damageable);
        }
    }
}