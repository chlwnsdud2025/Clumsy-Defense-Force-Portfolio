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

    public void Setup(float damage, Vector3 hitSpawnpointPosition) // 매개변수 이름을 알아보기 쉽게 변경
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
        transform.localScale = Vector3.one;
        textMesh.text = damage.ToString();
        textMesh.alpha = 1f;

        // 3. 목표 위치 계산 (여기도 랜덤을 섞어 이동하면서 더 사방으로 터지게)
        // 위로 올라가면서, 초기 위치 기준으로 targetOffsetRange만큼 더 퍼지게 계산
        float randomX = Random.Range(-targetOffsetRange, targetOffsetRange);
        float randomZ = Random.Range(-targetOffsetRange, targetOffsetRange);
        Vector3 targetPosition = initialPosition + new Vector3(randomX, moveDistance, randomZ);

        Renderer renderer = textMesh.GetComponent<Renderer>();
        if (renderer != null)
        {
            // 렌더링 큐를 Transparent(3000)보다 높은 값으로 설정하여 항상 나중에 그리게 함
            renderer.material.renderQueue = 4000;

            // Z-Test를 아예 끔 (깊이 체크 무시)
            renderer.material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        // 4. DoTween 시퀀스 생성 (이전과 동일)
        tweenSequence = DOTween.Sequence();

        tweenSequence.Append(transform.DOMove(targetPosition, duration).SetEase(Ease.OutBack));

        transform.localScale = Vector3.zero;
        tweenSequence.Join(transform.DOScale(Vector3.one, duration * 0.5f).SetEase(Ease.OutBounce));

        tweenSequence.Insert(duration * 0.5f, textMesh.DOFade(0f, duration * 0.5f));

        tweenSequence.OnComplete(() => {
            DamageTextPool.Instance.ReturnToPool(this.gameObject);
        });
    }
}