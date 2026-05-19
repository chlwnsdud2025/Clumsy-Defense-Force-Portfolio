using UnityEngine;

public class AuraWeapon : WeaponBase
{
    [Header("오라 데이터")]
    public AuraWeaponDataSO auraData;

    [Header("오라 설정")]
    public LayerMask enemyLayer;

    [Header("임시 테스트 스탯")]
    public float damage = 10f;
    public float cooldown = 0.5f;
    public float radius = 5f;

    private float attackTimer;
    private GameObject auraEffect;

    private void Start()
    {
        CreateAuraEffect();
    }

    private void Update()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer >= GetFinalCooldown(cooldown))
        {
            Attack();
            attackTimer = 0f;
        }

        UpdateAuraScale();
    }

    private void Attack()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                radius,
                enemyLayer
            );

        foreach (Collider hit in hits)
        {
            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            damageable.TakeDamage(
                GetFinalDamage(damage)
            );

            Debug.Log(
                $"[Aura] 데미지 : {GetFinalDamage(damage)}"
            );
        }
    }

    private void CreateAuraEffect()
    {
        if (auraData == null)
            return;

        if (auraData.auraEffectPrefab == null)
            return;

        auraEffect = Instantiate(
            auraData.auraEffectPrefab,
            transform
        );

        auraEffect.transform.localPosition =
            Vector3.zero;
    }

    private void UpdateAuraScale()
    {
        if (auraEffect == null)
            return;

        float size = radius * 2f;

        auraEffect.transform.localScale =
            new Vector3(
                size,
                1f,
                size
            );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            radius
        );
    }
}