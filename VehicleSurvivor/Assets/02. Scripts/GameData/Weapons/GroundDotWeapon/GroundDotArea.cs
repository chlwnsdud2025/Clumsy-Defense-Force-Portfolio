using System.Collections;
using UnityEngine;

public class GroundDotArea : MonoBehaviour
{
    [Header("장판 데이터")]
    public float damage;
    public float duration;
    public float tickInterval;
    public float width;
    public float length;

    public LayerMask enemyLayer;

    private void Start()
    {
        transform.localScale =
            new Vector3(width, 1f, length);

        StartCoroutine(DamageRoutine());

        Destroy(gameObject, duration);
    }

    public void Init(
        float newDamage,
        float newDuration,
        float newTickInterval,
        float newWidth,
        float newLength,
        LayerMask newEnemyLayer
    )
    {
        damage = newDamage;
        duration = newDuration;
        tickInterval = newTickInterval;
        width = newWidth;
        length = newLength;
        enemyLayer = newEnemyLayer;

        transform.localScale =
            new Vector3(width, 1f, length);
    }

    private IEnumerator DamageRoutine()
    {
        while (true)
        {
            DamageEnemies();

            yield return new WaitForSeconds(tickInterval);
        }
    }

    private void DamageEnemies()
    {
        Vector3 halfExtents =
            new Vector3(width * 0.5f, 1f, length * 0.5f);

        Collider[] hits = Physics.OverlapBox(
            transform.position,
            halfExtents,
            transform.rotation,
            enemyLayer
        );

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
                continue;

            Enemy enemy = hit.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);

                Debug.Log(
                    $"[Ground DoT] {enemy.name} 지속 피해 / 데미지: {damage}"
                );
            }
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