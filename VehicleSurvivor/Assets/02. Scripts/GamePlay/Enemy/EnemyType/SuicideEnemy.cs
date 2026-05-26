using UnityEngine;

public class SuicideEnemy : Enemy
{
    [Header("Suicide Settings")]
    public float fuseTime = 1.0f;
    public float explosionRadius = 3.0f;
    public float explosionDamage = 50f;

    [Header("3D Telegraph Settings")]
    public Transform warningSphere;
    public float sphereHeightOffset = 1.0f;

    [Header("Hit Settings")]
    public LayerMask playerLayer;

    // ==========================================
    // [추가됨] 자폭 폭발 이펙트 프리팹
    // ==========================================
    [Header("VFX")]
    public GameObject explosionEffectPrefab;

    // 보스에 의해 소환 되었을 시
    private bool _useSelfUpdate = false;


    private bool isFusing = false;
    private float fuseTimer;
    private float originalSeparation;

    public override void SpawnSetup(Transform target, Vector3 spawnPosition, EnemyManager manager)
    {
        base.SpawnSetup(target, spawnPosition, manager);
        isFusing = false;
        originalSeparation = separationWeight;
        isTrackingPlayer = true;

        if (warningSphere != null)
        {
            warningSphere.gameObject.SetActive(false);
            warningSphere.localScale = Vector3.zero;
        }
    }

    protected override void Attack()
    {
        if (isFusing) return;

        isFusing = true;
        fuseTimer = fuseTime;

        originalSeparation = separationWeight;
        separationWeight = 0f;
        isTrackingPlayer = false;

        if (warningSphere != null)
        {
            warningSphere.gameObject.SetActive(true);
            warningSphere.position = transform.position + Vector3.up * sphereHeightOffset;
            warningSphere.localScale = Vector3.zero;
        }

        Debug.Log("자폭병 점화! 3D 팽창 시작! 폭발 반경: " + explosionRadius);
    }

    protected override void ExecuteBehavior(float deltaTime, out Vector3 desiredVelocity)
    {
        if (!isFusing)
        {
            base.ExecuteBehavior(deltaTime, out desiredVelocity);
        }
        else
        {
            desiredVelocity = Vector3.zero;
            fuseTimer -= deltaTime;

            if (warningSphere != null)
            {
                float progress = 1.0f - (fuseTimer / fuseTime);
                float currentDiameter = Mathf.Lerp(0f, explosionRadius * 2f, progress);

                warningSphere.localScale = new Vector3(currentDiameter, currentDiameter, currentDiameter);
                warningSphere.position = transform.position + Vector3.up * sphereHeightOffset;
            }

            if (fuseTimer <= 0f)
            {
                Explode();
            }
        }
    }

    private void Explode()
    {

        if (warningSphere != null)
        {
            warningSphere.gameObject.SetActive(false);
        }

        Vector3 explosionCenter = transform.position + Vector3.up * sphereHeightOffset;

        // 터지는 타이밍에 이펙트 소환
        if (explosionEffectPrefab != null)
        {
            // fxParent 하위에 생성
            Transform parent = EnemyManager.Instance != null ? EnemyManager.Instance.FxParent : null;
            GameObject vfxInstance = Instantiate(explosionEffectPrefab, explosionCenter, Quaternion.identity, parent);

            // 3초 뒤 자동 삭제 예약
            Destroy(vfxInstance, 3.0f);
        }

        Collider[] hitColliders = Physics.OverlapSphere(explosionCenter, explosionRadius, playerLayer);

        foreach (Collider hit in hitColliders)
        {
            IDamageable target = hit.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(DamageInfo.Normal(explosionDamage));
            }
        }

        if (myManager != null)
        {
            myManager.ReturnToPool(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 center = transform.position + Vector3.up * sphereHeightOffset;
        Gizmos.DrawWireSphere(center, explosionRadius);
    }


    // 매니저의 추적 리스트에 들어가지 않고 보스 등에 의해 직접 Instantiate 된 경우


    public void EnableSelfUpdate()
    {
        _useSelfUpdate = true;
    }

    private void Update()
    {
        // 스스로 ManagedUpdate를 실행하여 플레이어를 정상 추격하게 만듭니다!
        if (_useSelfUpdate)
        {
            ManagedUpdate(Time.deltaTime);
        }
    }
}