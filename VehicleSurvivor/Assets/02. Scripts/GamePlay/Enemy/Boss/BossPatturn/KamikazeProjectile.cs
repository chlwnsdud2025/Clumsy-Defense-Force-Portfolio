using UnityEngine;
using DG.Tweening;

public class KamikazeProjectile : MonoBehaviour
{
    [Header("Explosion (Telegraph) Settings")]
    public GameObject telegraphPrefab;
    public GameObject explosionVfx;

    public float damage = 20f;
    public float explosionRadius = 3f;
    public float telegraphDuration = 0.8f;

    private LayerMask playerMask;
    private Vector3 originalScale;

    // 보스 머리 위에 나타날 때 호출
    public void Initialize(LayerMask layerMask)
    {
        playerMask = layerMask;

        originalScale = transform.localScale;
        transform.localScale = Vector3.zero;

        // 뿅 하고 커지며 등장
        transform.DOScale(originalScale, 0.4f).SetEase(Ease.OutBack);
    }

    // 보스가 계산한 좌표로 강제 순간이동 후 퓨즈 점화!
    public void TeleportAndFuse(Vector3 targetPos)
    {
        // 1. 공중/바닥 높이 보정
        Vector3 spawnPos = targetPos;
        if (Physics.Raycast(targetPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 50f))
        {
            spawnPos = hit.point;
        }

        // 2. 순간이동 (바닥에서 살짝 위)
        transform.position = spawnPos + Vector3.up * 0.5f;

        // 순간이동 연출 (순식간에 작아졌다가 다시 뿅!)
        transform.localScale = Vector3.zero;
        transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutQuad);

        // 3. 바닥에 예고 장판(Telegraph) 깔기
        if (telegraphPrefab != null)
        {
            GameObject telegraphObj = Instantiate(telegraphPrefab, spawnPos, Quaternion.identity, transform.parent);
            BossTelegraph telegraph = telegraphObj.GetComponent<BossTelegraph>();

            if (telegraph != null)
            {
                telegraph.explosionVfxPrefab = explosionVfx;
                telegraph.Initialize(damage, explosionRadius, telegraphDuration, playerMask);
            }
        }

        // 4. [핵심 수정] 0.2초 동안 커진 이후에(SetDelay) 부들부들 떨기 시작하도록 변경!
        // (전체 시간에서 커지는 시간 0.2초를 뺀 만큼만 떱니다)
        transform.DOShakeScale(telegraphDuration - 0.2f, 0.5f, 10, 90f).SetDelay(0.2f);

        // 장판 시간에 맞춰 삭제
        Invoke(nameof(SelfDestroy), telegraphDuration);
    }

    private void SelfDestroy()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // 이 오브젝트의 Transform에서 돌아가고 있는 모든 DOTween 애니메이션 강제 종료
        transform.DOKill();
    }
}