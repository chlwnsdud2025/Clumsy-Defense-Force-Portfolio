using System.Collections;
using UnityEngine;

public class GroundDotArea : MonoBehaviour
{
    private float damage;
    private float duration;
    private float tickInterval;
    private float width;
    private float length;

    public void Initialize(
        float newDamage,
        float newDuration,
        float newTickInterval,
        float newWidth,
        float newLength
    )
    {
        damage = newDamage;
        duration = newDuration;
        tickInterval = newTickInterval;
        width = newWidth;
        length = newLength;

        transform.localScale =
            new Vector3(
                width,
                transform.localScale.y,
                length
            );

        StartCoroutine(DamageRoutine());

        Destroy(gameObject, duration);
    }

    private IEnumerator DamageRoutine()
    {
        while (true)
        {
            DamageTargets();

            yield return new WaitForSeconds(tickInterval);
        }
    }

    private void DamageTargets()
    {
        Vector3 halfExtents =
            new Vector3(
                width * 0.5f,
                1f,
                length * 0.5f
            );

        Collider[] hits = Physics.OverlapBox(
            transform.position,
            halfExtents,
            transform.rotation
        );

        foreach (Collider hit in hits)
        {
            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            damageable.TakeDamage(damage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.matrix = Matrix4x4.TRS(
            transform.position,
            transform.rotation,
            Vector3.one
        );

        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(width, 2f, length)
        );
    }
}