using UnityEngine;

public class AuraWeapon : WeaponBase
{
    [Header("오라 설정")]
    public LayerMask enemyLayer;

    [Header("이펙트 설정")]
    public GameObject auraEffectPrefab;

    private GameObject auraEffectInstance;

    protected override void Awake()
    {
        base.Awake();

        CreateAuraEffect();
    }

    protected override void Attack(WeaponLevelData data)
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            data.radius,
            enemyLayer
        );

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
                continue;

            Enemy enemy = hit.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                enemy.TakeDamage(
                    GetFinalDamage(data.damage)
                );

                Debug.Log(
                    $"[Aura] {enemy.name} 피격 / 데미지: {GetFinalDamage(data.damage)}"
                );
            }
        }

        UpdateAuraEffectScale(data.radius);
    }

    private void CreateAuraEffect()
    {
        if (auraEffectPrefab == null)
            return;

        auraEffectInstance = Instantiate(
            auraEffectPrefab,
            transform.position,
            Quaternion.identity,
            transform
        );

        auraEffectInstance.transform.localPosition = Vector3.zero;
        auraEffectInstance.transform.localRotation = Quaternion.identity;
        auraEffectInstance.transform.localScale = Vector3.one;
    }

    private void UpdateAuraEffectScale(float radius)
    {
        if (auraEffectInstance == null)
            return;

        float diameter = radius * 2f;

        auraEffectInstance.transform.localScale =
            new Vector3(diameter, 1f, diameter);
    }

    private void OnDestroy()
    {
        if (auraEffectInstance != null)
        {
            Destroy(auraEffectInstance);
        }
    }

    private void OnDrawGizmosSelected()
    {
        WeaponLevelData data = GetCurrentLevelData();

        if (data == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            transform.position,
            data.radius
        );
    }
}