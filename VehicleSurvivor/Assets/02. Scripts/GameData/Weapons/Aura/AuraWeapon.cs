using UnityEngine;

public class AuraWeapon : WeaponBase
{
    // Damage = 틱당 데미지
    // Cooldown = 데미지 주기
    // Range = 오라 반경
    // Size = Range가 0일 때 대체 반경

    [Header("Aura References")]
    public GameObject auraEffectPrefab;

    private GameObject auraEffectInstance;

    protected override void OnInitialized()
    {
        CreateAuraEffect();
        UpdateAuraFromLevelData();
    }

    private void Start()
    {
        CreateAuraEffect();
        UpdateAuraFromLevelData();
    }

    protected override void OnLevelDataChanged(WeaponLevelData data)
    {
        UpdateAuraFromLevelData();
    }

    protected override void Attack(WeaponLevelData data)
    {
        if (data == null)
            return;

        float radius = GetAuraRadius(data);
        // 변경된 DamageInfo 시스템 적용
        DamageInfo damageInfo = GetFinalDamage(data.Damage);

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            radius
        );

        foreach (Collider hit in hits)
        {
            // [수정] 플레이어 레이어 제외
            if (IsPlayerLayer(hit))
                continue;

            IDamageable damageable =
                hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            damageable.TakeDamage(damageInfo);
            Debug.Log("오라 데미지 들어감");
        }

        UpdateAuraScale(radius);
    }

    private void CreateAuraEffect()
    {
        if (auraEffectPrefab == null)
        {
            Debug.LogWarning("[AuraWeapon] Aura Effect Prefab이 비어있습니다.");
            return;
        }

        if (auraEffectInstance != null)
            return;

        auraEffectInstance = Instantiate(
            auraEffectPrefab,
            transform
        );

        auraEffectInstance.transform.localPosition = Vector3.zero;
        auraEffectInstance.transform.localRotation =
            auraEffectPrefab.transform.rotation;

        auraEffectInstance.SetActive(true);
    }

    private void UpdateAuraFromLevelData()
    {
        WeaponLevelData data = GetCurrentLevelData();

        if (data == null)
            return;

        UpdateAuraScale(GetAuraRadius(data));
    }

    private void UpdateAuraScale(float radius)
    {
        if (auraEffectInstance == null)
            return;

        float size = radius * 2f;

        auraEffectInstance.transform.localScale =
            new Vector3(size, size, size);
    }

    private float GetAuraRadius(WeaponLevelData data)
    {
        float baseRadius = data.Range > 0f
            ? data.Range
            : data.Size;

        return GetFinalWeaponScale(baseRadius);
    }

    // [추가] 플레이어 판별 로직
    private bool IsPlayerLayer(Collider other)
    {
        int playerLayer = LayerMask.NameToLayer("Player");

        if (playerLayer == -1)
            return false;

        if (other.gameObject.layer == playerLayer)
            return true;

        Transform root = other.transform.root;
        return root.gameObject.layer == playerLayer;
    }

    private void OnDrawGizmosSelected()
    {
        WeaponLevelData data = GetCurrentLevelData();

        if (data == null)
            return;

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            GetAuraRadius(data)
        );
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (auraEffectInstance != null)
        {
            Destroy(auraEffectInstance);
        }
    }
}