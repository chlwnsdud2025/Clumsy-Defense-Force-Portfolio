using System.Collections;
using UnityEngine;

public class BossTelegraph : MonoBehaviour
{
    private float damage;
    private float radius;
    private float telegraphTime;
    private LayerMask targetLayer;

    public Transform indicatorMesh;

    // [핵심 추가] 보스에게서 넘겨받을 폭발 이펙트 프리팹
    [HideInInspector] public GameObject explosionVfxPrefab;

    public void Initialize(float damage, float radius, float telegraphTime, LayerMask targetLayer)
    {
        this.damage = damage;
        this.radius = radius;
        this.telegraphTime = telegraphTime;
        this.targetLayer = targetLayer;

        StartCoroutine(TelegraphAndExplode());
    }

    private IEnumerator TelegraphAndExplode()
    {
        float timer = 0f;

        // 시작 크기를 0으로 초기화
        if (indicatorMesh != null) indicatorMesh.localScale = Vector3.zero;

        // 1. 장판 팽창 연출
        while (timer < telegraphTime)
        {
            timer += Time.deltaTime;
            if (indicatorMesh != null)
            {
                float progress = timer / telegraphTime;
                float currentSize = Mathf.Lerp(0f, radius * 2f, progress);

                if (indicatorMesh.parent != null)
                {
                    Vector3 pScale = indicatorMesh.parent.lossyScale;
                    indicatorMesh.localScale = new Vector3(currentSize / pScale.x, currentSize / pScale.y, currentSize / pScale.z);
                }
                else
                {
                    indicatorMesh.localScale = new Vector3(currentSize, currentSize, currentSize);
                }
            }
            yield return null;
        }

        // 타격 판정
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, targetLayer);
        foreach (Collider hit in hits)
        {
            hit.GetComponent<IDamageable>()?.TakeDamage(damage);
        }

        // 데미지 판정과 완벽히 동일한 타이밍에 폭발 이펙트 생성!
        if (explosionVfxPrefab != null)
        {
            // 장판의 위치(바닥)에 이펙트 생성
            Instantiate(explosionVfxPrefab, transform.position, Quaternion.identity);
        }

        // 4. 폭발 연출 후 장판 삭제
        if (indicatorMesh != null) indicatorMesh.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.2f);
        Destroy(gameObject);
    }
}