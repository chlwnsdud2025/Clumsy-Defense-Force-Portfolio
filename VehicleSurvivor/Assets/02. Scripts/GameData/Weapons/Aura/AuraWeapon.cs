using UnityEngine;

public class AuraWeapon : WeaponBase
{
    [Header("Aura References")]
    public GameObject auraEffectPrefab;

    [Header("Aura Settings")]
    public LayerMask enemyLayer;

    private GameObject auraEffect;

    private void Start()
    {
        CreateAuraEffect();
    }

    protected override void Update()
    {
        base.Update();
        UpdateAuraScale(GetAuraRadius(GetCurrentLevelData()));
    }

    protected override void Attack(WeaponLevelData data)
    {
        DealAuraDamage(
            GetFinalDamage(data.Damage),
            GetAuraRadius(data)
        );
    }

    private void DealAuraDamage(float finalDamage, float attackRadius)
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            attackRadius,
            enemyLayer
        );

        foreach (Collider hit in hits)
        {
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            damageable.TakeDamage(finalDamage);
            Debug.Log($"[Aura] damage: {finalDamage}");
        }
    }

    private void CreateAuraEffect()
    {
        if (auraEffectPrefab == null)
            return;

        auraEffect = Instantiate(auraEffectPrefab, transform);
        auraEffect.transform.localPosition = Vector3.zero;
    }

    private void UpdateAuraScale(float attackRadius)
    {
        if (auraEffect == null)
            return;

        float size = attackRadius * 2f;
        auraEffect.transform.localScale = new Vector3(size, 1f, size);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, GetAuraRadius(GetCurrentLevelData()));
    }

    private float GetAuraRadius(WeaponLevelData data)
    {
        if (data == null)
            return 0f;

        return data.Range > 0f ? data.Range : data.Radius;
    }
}
