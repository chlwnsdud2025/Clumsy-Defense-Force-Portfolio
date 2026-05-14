using UnityEngine;

public class AuraWeapon : WeaponBase
{
    [Header("오라 설정")]
    public LayerMask enemyLayer;

    [Header("이펙트 설정")]
    public GameObject auraEffectPrefab;

    private GameObject auraEffect;

    protected override void Awake()
    {
        base.Awake();

        if (auraEffectPrefab != null)
        {
            auraEffect = Instantiate(
                auraEffectPrefab,
                transform.position,
                Quaternion.identity,
                transform
            );

            auraEffect.transform.localPosition = Vector3.zero;
        }
    }

    protected override void Attack(WeaponLevelData data)
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            data.radius,
            enemyLayer
        );

        float finalDamage = GetFinalDamage(data.damage);

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
                continue;

            Enemy enemy = hit.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                enemy.TakeDamage(finalDamage);
                Debug.Log("데미지를 주었습니다");
            }
        }
    }

    protected override void OnWeaponLevelUp()
    {
        base.OnWeaponLevelUp();
        UpdateAuraEffectScale();
    }

    private void Start()
    {
        UpdateAuraEffectScale();
    }

    private void UpdateAuraEffectScale()
    {
        WeaponLevelData data = GetCurrentLevelData();

        if (data == null || auraEffect == null)
            return;

        auraEffect.transform.localScale =
            new Vector3(data.radius * 2f, 0.1f, data.radius * 2f);
    }

    private void OnDrawGizmosSelected()
    {
        WeaponLevelData data = GetCurrentLevelData();

        if (data == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, data.radius);
    }
}