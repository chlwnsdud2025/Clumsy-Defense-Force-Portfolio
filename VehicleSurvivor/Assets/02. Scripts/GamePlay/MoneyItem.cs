using UnityEngine;
using DG.Tweening;

public class MoneyItem : MonoBehaviour
{
    [Header("Optional VFX")]
    [Tooltip("사라질 때 터질 반짝임 파티클 프리팹 (선택사항)")]
    public GameObject sparkleEffectPrefab;

    private int moneyAmount;
    private MoneyManager myManager;

    public void Setup(MoneyManager manager)
    {
        myManager = manager;
    }

    public void Spawn(Vector3 startPos, int amount, Transform player)
    {
        moneyAmount = amount;

        transform.position = startPos;
        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;

        gameObject.SetActive(true);

        Vector3 randomOffset = new Vector3(Random.Range(-2.5f, 2.5f), 0, Random.Range(-2.5f, 2.5f));
        Vector3 jumpTarget = startPos + randomOffset;
        jumpTarget.y = startPos.y;

        Sequence seq = DOTween.Sequence();

        seq.Append(transform.DOJump(jumpTarget, 3f, 1, 0.6f).SetEase(Ease.OutQuad));
        seq.Join(transform.DORotate(new Vector3(0, 1080, 0), 0.6f, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
        seq.Append(transform.DORotate(new Vector3(0, 1800, 0), 0.4f, RotateMode.FastBeyond360).SetEase(Ease.Linear));
        seq.Append(transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));

        seq.OnComplete(() =>
        {
            if (sparkleEffectPrefab != null)
            {
                // [핵심 추가] Runtime_VFX_Container 폴더를 찾고, 없으면 하나 만듭니다!
                GameObject vfxContainer = GameObject.Find("Runtime_VFX_Container");
                if (vfxContainer == null)
                {
                    vfxContainer = new GameObject("Runtime_VFX_Container");
                }

                // [핵심 추가] 해당 폴더(Container)의 자식으로 이펙트를 생성하여 하이어라키를 깨끗하게 유지합니다.
                GameObject sparkle = Instantiate(sparkleEffectPrefab, transform.position, Quaternion.identity, vfxContainer.transform);

                // [핵심 추가] 파티클 생성 후 2.0초 뒤에 무조건 메모리에서 삭제합니다!
                // (만약 파티클 길이가 길다면 2.0f를 3.0f 등으로 늘려주세요)
                Destroy(sparkle, 2.0f);
            }

            Collect();
        });
    }

    private void Collect()
    {
        // ==========================================
        // TODO: 여기서 플레이어 재화 증가 함수를 호출하세요!
        // ==========================================
        Debug.Log($"<color=yellow>돈 {moneyAmount}G 획득!</color> (연출 종료)");

        myManager.ReturnToPool(this);
    }
}