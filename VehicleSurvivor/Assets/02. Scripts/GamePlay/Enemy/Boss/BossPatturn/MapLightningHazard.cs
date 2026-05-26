using System.Collections;
using UnityEngine;

public class MapLightningHazard : MonoBehaviour
{
    [Header("Spawn Area Settings")]
    [Tooltip("낙뢰가 떨어질 중심점 (비워두면 이 스크립트가 붙은 오브젝트의 위치를 사용합니다)")]
    public Transform mapCenter;
    public float spawnRadius = 20f;
    public float raycastHeight = 100f;
    public LayerMask groundLayer;

    [Header("Sequence Settings (다중 순차 소환)")]
    [Tooltip("한 사이클에 몇 개의 번개를 연속으로 부를 것인지")]
    public int spawnCount = 3;
    [Tooltip("각 번개가 나타나는 시간 간격 (예: 0.2초마다 하나씩 등장)")]
    public float sequenceDelay = 0.2f;

    [Header("Hazard Settings")]
    [Tooltip("다중 소환 사이클이 몇 초마다 반복될 것인지")]
    public float spawnInterval = 4.0f;
    [Tooltip("구체형 인디케이터가 생성되고 실제 번개가 떨어지기까지의 대기 시간")]
    public float warningDuration = 1.5f;
    public float damage = 20f;
    [Tooltip("이 반경 크기에 맞춰 구체형 인디케이터의 크기가 자동으로 조절됩니다.")]
    public float explosionRadius = 3f;
    public LayerMask targetLayer; // 플레이어가 포함된 레이어

    [Header("VFX Prefabs")]
    public GameObject sphereIndicatorPrefab;
    public GameObject lightningStrikePrefab;

    private float timer;
    private Transform cachedVfxContainer = null; // [추가] 하이어라키 정리용 컨테이너 캐싱

    private void OnEnable()
    {
        timer = 0f;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            StartCoroutine(LightningSequenceRoutine());
        }
    }

    private IEnumerator LightningSequenceRoutine()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            TrySpawnSingleLightning();

            if (sequenceDelay > 0f)
            {
                yield return new WaitForSeconds(sequenceDelay);
            }
        }
    }

    private void TrySpawnSingleLightning()
    {
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 center = mapCenter != null ? mapCenter.position : transform.position;
        Vector3 rayStart = new Vector3(center.x + randomCircle.x, center.y + raycastHeight, center.z + randomCircle.y);

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayer))
        {
            StartCoroutine(LightningRoutine(hit.point));
        }
    }

    private IEnumerator LightningRoutine(Vector3 targetPos)
    {
        // ==========================================
        // 🛠️ STEP 1: 구체형 인디케이터 소환 및 크기 맞춤
        // ==========================================
        GameObject indicator = null;
        if (sphereIndicatorPrefab != null)
        {
            // [수정] 맵 자식이 아니라 다른 보스들처럼 전용 VFX 폴더(GetVfxContainer())로 정렬시킵니다!
            indicator = Instantiate(sphereIndicatorPrefab, targetPos, Quaternion.identity, GetVfxContainer());

            // [핵심 해결] 구체의 반지름(Radius) 기준이므로, 지름 크기에 맞게 localScale을 변환해 줍니다 (반지름 * 2)
            float diameter = explosionRadius * 2f;
            indicator.transform.localScale = new Vector3(diameter, diameter, diameter);
        }

        yield return new WaitForSeconds(warningDuration);

        // ==========================================
        // 🛠️ STEP 2: 인디케이터 제거 및 번개 타격
        // ==========================================
        if (indicator != null) Destroy(indicator);

        if (lightningStrikePrefab != null)
        {
            // [수정] 번개 이펙트도 전용 VFX 폴더로 들어가도록 수정하여 하이어라키를 청소합니다.
            GameObject lightning = Instantiate(lightningStrikePrefab, targetPos, Quaternion.identity, GetVfxContainer());
            Destroy(lightning, 2.0f);
        }

        // ==========================================
        // 🛠️ STEP 3: 데미지 판정
        // ==========================================
        Collider[] hits = Physics.OverlapSphere(targetPos, explosionRadius, targetLayer);
        foreach (Collider hitCol in hits)
        {
            IDamageable damageable = hitCol.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
        }
    }

    // ==========================================
    // 📁 [핵심 추가] 다른 보스들과 동일한 전용 폴더 추적 관리 기능
    // ==========================================
    private Transform GetVfxContainer()
    {
        if (cachedVfxContainer != null) return cachedVfxContainer;

        // EnemyManager의 FxParent(Runtime_VFX_Container)가 있다면 최우선적으로 귀속시킵니다.
        if (EnemyManager.Instance != null && EnemyManager.Instance.FxParent != null)
        {
            cachedVfxContainer = EnemyManager.Instance.FxParent;
            return cachedVfxContainer;
        }

        // 없다면 독립된 보스전용 컨테이너를 찾거나 생성합니다.
        GameObject container = GameObject.Find("Boss_VFX_Container");
        if (container == null) container = new GameObject("Boss_VFX_Container");
        cachedVfxContainer = container.transform;
        return cachedVfxContainer;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = mapCenter != null ? mapCenter.position : transform.position;
        Gizmos.DrawWireSphere(center, spawnRadius);
    }
}