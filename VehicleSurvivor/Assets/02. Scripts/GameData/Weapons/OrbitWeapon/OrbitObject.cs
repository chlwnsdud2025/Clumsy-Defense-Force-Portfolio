using System.Collections.Generic;
using UnityEngine;

public class OrbitObject : MonoBehaviour
{
    private float damage;
    private float hitCooldown;

    private readonly Dictionary<IDamageable, float> hitTimers =
        new Dictionary<IDamageable, float>();

    public void Initialize(
        float newDamage,
        float newHitCooldown
    )
    {
        damage = newDamage;
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

        damageable.TakeDamage(damage);

        Debug.Log($"[OrbitObject] 데미지: {damage}");
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