using UnityEngine;
using UnityEngine.Rendering.Universal; // DecalProjector 사용

public class SuicideEnemy : Enemy
{
    [Header("Suicide Settings")]
    public float fuseTime = 1.0f;
    public float explosionRadius = 3.0f;
    public float explosionDamage = 50f;

    [Header("Decal Settings")]
    public DecalProjector suicideDecal; // 자식 오브젝트의 데칼 할당
    public float decalDepth = 5.0f;     // 바닥 투사 깊이

    private bool isFusing = false;
    private float fuseTimer;
    private float originalSeparation;

    // 매터리얼 독립화 캐싱 변수
    private Material decalMaterialInstance;

    public override void SpawnSetup(Transform target, Vector3 spawnPosition, EnemyManager manager)
    {
        base.SpawnSetup(target, spawnPosition, manager);
        isFusing = false;
        originalSeparation = separationWeight;
        isTrackingPlayer = true;

        if (suicideDecal == null)
        {
            suicideDecal = GetComponentInChildren<DecalProjector>(true);
        }

        // 데칼 초기화 및 매터리얼 독립화
        if (suicideDecal != null)
        {
            if (decalMaterialInstance == null && suicideDecal.material != null)
            {
                decalMaterialInstance = new Material(suicideDecal.material);
                suicideDecal.material = decalMaterialInstance;
            }
            suicideDecal.gameObject.SetActive(false);
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

        // 데칼 장판 켜기
        if (suicideDecal != null && decalMaterialInstance != null)
        {
            suicideDecal.gameObject.SetActive(true);

            suicideDecal.transform.position = transform.position;

            suicideDecal.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

            // 크기: 반지름(Radius) 기준이므로 직경(Diameter)은 x2 입니다. -> ?
            float diameter = explosionRadius * 2f;
            suicideDecal.size = new Vector3(diameter, diameter, decalDepth);

            // 쉐이더 값 초기화
            decalMaterialInstance.SetFloat("_FillAmount", 0f);
        }

        Debug.Log("자폭병 점화! 범위: " + explosionRadius);
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

            // 데칼 차오르는 쉐이더 연출
            if (suicideDecal != null && decalMaterialInstance != null)
            {
                float progress = 1.0f - (fuseTimer / fuseTime);
                decalMaterialInstance.SetFloat("_FillAmount", progress);
            }

            if (fuseTimer <= 0f)
            {
                Explode();
            }
        }
    }

    private void Explode()
    {
        Debug.Log("자폭 쾅!");

        // 데칼 숨기기
        if (suicideDecal != null)
        {
            suicideDecal.gameObject.SetActive(false);
        }

        

        // 자폭 시 경험치 X
        if (myManager != null)
        {
            myManager.ReturnToPool(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // 메모리 누수 방지
    private void OnDestroy()
    {
        if (decalMaterialInstance != null)
        {
            Destroy(decalMaterialInstance);
        }
    }
}