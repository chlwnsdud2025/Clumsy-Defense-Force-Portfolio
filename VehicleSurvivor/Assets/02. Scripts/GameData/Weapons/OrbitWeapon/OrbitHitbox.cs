using System.Collections.Generic;
using UnityEngine;

public class OrbitHitbox : MonoBehaviour
{
    private float damage;
    private float hitCooldown;
    private GameObject hitEffectPrefab;

    private readonly Dictionary<IDamageable, float> hitTimers =
        new Dictionary<IDamageable, float>();

    public void Init(
        float newDamage,
        float newHitCooldown,
        GameObject newHitEffectPrefab
    )
    {
        damage = newDamage;
        hitCooldown = newHitCooldown;
        hitEffectPrefab = newHitEffectPrefab;
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

        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                hitEffectPrefab,
                other.transform.position,
                Quaternion.identity
            );

            Destroy(effect, 2f);
        }

        Debug.Log($"[Orbit] 데미지 : {damage}");
    }
}