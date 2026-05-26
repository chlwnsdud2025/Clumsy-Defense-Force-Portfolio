using UnityEngine;
using TMPro;
using DG.Tweening;

public class DamageText : MonoBehaviour
{
    public TextMeshPro textMesh;

    [Header("Animation Settings")]
    public float moveDistance = 1.5f; // 위로 튀어오를 거리
    public float duration = 0.6f;     // 애니메이션 전체 길이

    [Header("Random Settings")]
    public float spawnOffsetRange = 0.3f; // 초기 생성 위치의 랜덤 범위
    public float targetOffsetRange = 0.5f; // 도착 위치의 랜덤 범위 (이동하면서 더 퍼지게)

    private Sequence tweenSequence;

    [Header("Critical Settings")]
    public Color criticalColor = Color.yellow;
    public float criticalScaleMultiplier = 1.3f;

    public void Setup(float damage, Vector3 hitSpawnpointPosition, bool isCritical = false)
    {
        // 1. 기존 트윈 강제 종료
        tweenSequence?.Kill();

        // 몬스터의 Spawnpoint 기준으로 작은 구형 범위 안에서 랜덤 위치를 계산합니다.
        // Random.insideUnitSphere는 반지름이 1인 구 내부의 랜덤 벡터를 반환합니다.
        Vector3 randomSpawnOffset = Random.insideUnitSphere * spawnOffsetRange;
        Vector3 initialPosition = hitSpawnpointPosition + randomSpawnOffset;

        // 텍스트의 초기 위치를 랜덤화된 위치로 세팅합니다.
        transform.position = initialPosition;
        // ----------------------------------------

        // 2. 상태 초기화
        transform.localScale = isCritical
            ? Vector3.one * criticalScaleMultiplier
            : Vector3.one;
        int displayDamage = Mathf.RoundToInt(damage);
        textMesh.text = isCritical ? $"{displayDamage}!" : displayDamage.ToString();
        textMesh.color = isCritical ? criticalColor : Color.white;
        textMesh.alpha = 1f;

        // 3. 목표 위치 계산 (여기도 랜덤을 섞어 이동하면서 더 사방으로 터지게)
        // 위로 올라가면서, 초기 위치 기준으로 targetOffsetRange만큼 더 퍼지게 계산
        float randomX = Random.Range(-targetOffsetRange, targetOffsetRange);
        float randomZ = Random.Range(-targetOffsetRange, targetOffsetRange);
        Vector3 targetPosition = initialPosition + new Vector3(randomX, moveDistance, randomZ);

        Renderer textRenderer = textMesh.GetComponent<Renderer>();
        if (textRenderer != null)
        {
            Material mat = textRenderer.material;

            // 렌더링 큐 최상단 배치
            mat.renderQueue = 4000;

            // _ZTest가 아니라 _ZTestMode 입니다! (8은 Always를 의미)
            mat.SetInt("_ZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        // 4. DoTween 시퀀스 생성 (이전과 동일)
        tweenSequence = DOTween.Sequence();

        tweenSequence.Append(transform.DOMove(targetPosition, duration).SetEase(Ease.OutBack));

        transform.localScale = Vector3.zero;
        Vector3 targetScale = isCritical
            ? Vector3.one * criticalScaleMultiplier
            : Vector3.one;
        tweenSequence.Join(transform.DOScale(targetScale, duration * 0.5f).SetEase(Ease.OutBounce));

        tweenSequence.Insert(duration * 0.5f, textMesh.DOFade(0f, duration * 0.5f));

        tweenSequence.OnComplete(() => {
            DamageTextPool.Instance.ReturnToPool(this.gameObject);
        });
    }
}