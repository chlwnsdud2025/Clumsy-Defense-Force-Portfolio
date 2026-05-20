using UnityEngine;

public class AuraWeapon : WeaponBase
{
    [Header("Aura Data")]
    public AuraWeaponDataSO auraData;

    [Header("Aura Settings")]
    public LayerMask enemyLayer;

    [Header("Legacy Stats")]
    public float damage = 10f;
    public float cooldown = 0.5f;
    public float radius = 5f;

    private GameObject auraEffect;

    private void Start()
    {
        CreateAuraEffect();
    }

    protected override void Update()
    {
        if (HasConfiguredLevelData())
        {
            base.Update();
            UpdateAuraScale(GetAuraRadius(GetCurrentLevelData()));
            return;
        }

        attackTimer += Time.deltaTime;

        if (attackTimer >= GetFinalCooldown(cooldown))
        {
            AttackLegacy();
            attackTimer = 0f;
        }

        UpdateAuraScale(radius);
    }

    protected override void Attack(WeaponLevelData data)
    {
        DealAuraDamage(
            GetFinalDamage(data.Damage),
            GetAuraRadius(data)
        );
    }

    private void AttackLegacy()
    {
        DealAuraDamage(
            GetFinalDamage(damage),
            radius
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
        if (auraData == null || auraData.auraEffectPrefab == null)
            return;

        auraEffect = Instantiate(auraData.auraEffectPrefab, transform);
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
            return radius;

        return data.Range > 0f ? data.Range : data.radius;
    }
}
