using System.Collections.Generic;
using UnityEngine;

public class PiercingRaycastWeapon : WeaponBase
{
    [Header("레이저 설정")]
    public Transform firePoint;
    public LayerMask enemyLayer;

    [Header("이펙트 설정")]
    public LineRenderer lineRenderer;
    public float lineShowTime = 0.05f;

    protected override void Attack(WeaponLevelData data)
    {
        Transform resolvedFirePoint = ResolveFirePoint(firePoint);

        if (resolvedFirePoint == null)
            return;

        Vector3 start = resolvedFirePoint.position;
        Vector3 direction = resolvedFirePoint.forward;

        float range = GetFinalWeaponScale(data.Radius);

        RaycastHit[] hits = Physics.RaycastAll(
            start,
            direction,
            range,
            enemyLayer
        );

        List<Enemy> damagedEnemies =
            new List<Enemy>();

        foreach (RaycastHit hit in hits)
        {
            if (!hit.collider.CompareTag("Enemy"))
                continue;

            Enemy enemy =
                hit.collider.GetComponentInParent<Enemy>();

            if (enemy == null)
                continue;

            if (damagedEnemies.Contains(enemy))
                continue;

            enemy.TakeDamage(
                GetFinalDamage(data.Damage)
            );

            damagedEnemies.Add(enemy);

            Debug.Log(
                $"[Laser] {enemy.name} 피격 / 데미지: {GetFinalDamage(data.Damage)}"
            );
        }

        ShowLine(
            start,
            start + direction * range
        );
    }

    private void ShowLine(
        Vector3 start,
        Vector3 end
    )
    {
        if (lineRenderer == null)
            return;

        lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        CancelInvoke(nameof(HideLine));
        Invoke(nameof(HideLine), lineShowTime);
    }

    private void HideLine()
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }
}
