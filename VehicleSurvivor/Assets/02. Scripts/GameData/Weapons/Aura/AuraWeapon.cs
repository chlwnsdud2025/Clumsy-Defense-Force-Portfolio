using UnityEngine;

public class AuraWeapon : WeaponBase
{
    // Damage = 틱당 데미지
    // Cooldown = 데미지 주기
    // Radius = 오라의 실제 장판 크기 (반경)

    [Header("Aura References")]
    public GameObject auraEffectPrefab;

    [Header("Visual Option")]
    [Tooltip("실제 데미지 판정(Radius)과 시각적 이펙트의 크기를 일치시키기 위한 배율입니다. (기본값: 2)")]
    public float visualScaleMultiplier = 2f;

    // 🌟 [수정됨] 절대 높이가 아닌, 현재 무기 위치에서 Y축을 얼마나 내릴지 결정하는 '상대값' 변수로 변경
    [Tooltip("무기 위치에서 장판을 얼마나 아래로 내릴지 설정합니다. (예: -1.0을 넣으면 무기보다 1m 아래에 깔림)")]
    public float yOffset = -0.5f;

    private GameObject auraEffectInstance;
    private float originalYScale = 1f;

    protected override void OnInitialized()
    {
        CreateAuraEffect();
        UpdateAuraFromLevelData();
    }

    protected override void OnLevelDataChanged(WeaponLevelData data)
    {
        UpdateAuraFromLevelData();
    }

    protected override void Update()
    {
        base.Update();

        if (auraEffectInstance != null)
        {
            // 🌟 [핵심 수정] 무기의 현재 위치를 그대로 받아오고, Y축만 상대값(yOffset)만큼 더해줍니다. (언덕 등반 완벽 반영)
            Vector3 centerPosition = transform.position;
            centerPosition.y += yOffset;

            auraEffectInstance.transform.position = centerPosition;
        }
    }

    protected override void Attack(WeaponLevelData data)
    {
        if (data == null)
            return;

        float radius = GetAuraRadius(data);
        DamageInfo damageInfo = GetFinalDamage(data.Damage);

        // 🌟 [핵심 수정] 판정 구체의 위치도 플레이어의 현재 Y축을 동기화하여 상대적으로 내려줍니다.
        Vector3 centerPosition = transform.position;
        centerPosition.y += yOffset;

        Collider[] hits = Physics.OverlapSphere(
            centerPosition,
            radius
        );

        foreach (Collider hit in hits)
        {
            if (IsPlayerLayer(hit))
                continue;

            if (!hit.CompareTag("Enemy"))
                continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            damageable.TakeDamage(damageInfo);
            Debug.Log($"[AuraWeapon] 오라 데미지 들어감: {damageInfo.Amount}");
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

        auraEffectInstance = Instantiate(auraEffectPrefab);

        originalYScale = auraEffectPrefab.transform.localScale.y;

        Vector3 initialPosition = transform.position;
        initialPosition.y += yOffset;

        auraEffectInstance.transform.position = initialPosition;
        auraEffectInstance.transform.rotation = auraEffectPrefab.transform.rotation;

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

        float size = radius * visualScaleMultiplier;

        auraEffectInstance.transform.localScale = new Vector3(size, originalYScale, size);
    }

    private float GetAuraRadius(WeaponLevelData data)
    {
        float baseRadius = data.Radius > 0f ? data.Radius : (data.Range > 0f ? data.Range : data.Size);
        return GetFinalWeaponScale(baseRadius);
    }

    private bool IsPlayerLayer(Collider other)
    {
        int playerLayer = LayerMask.NameToLayer("Player");

        if (playerLayer == -1)
            return false;

        Transform currentTransform = other.transform;

        while (currentTransform != null)
        {
            if (currentTransform.gameObject.layer == playerLayer)
                return true;

            currentTransform = currentTransform.parent;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        WeaponLevelData data = GetCurrentLevelData();

        if (data == null)
            return;

        Gizmos.color = Color.red;

        Vector3 centerPosition = transform.position;
        centerPosition.y += yOffset;

        Gizmos.DrawWireSphere(
            centerPosition,
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