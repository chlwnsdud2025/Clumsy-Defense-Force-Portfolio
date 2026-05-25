using System.Collections;
using System.Collections.Generic; // List<>를 사용하기 위해 반드시 필요합니다!
using UnityEngine;
using DG.Tweening; // DOTween 사용을 위해 필수!

public class Phantom_Boss : BossEnemy
{
    [Header("Targeting")]
    public LayerMask playerLayerMask;

    [Header("Pattern: Teleport Strike (순간이동 강타)")]
    [Tooltip("애니메이션을 적용할 보스의 실제 모델링 (Mesh) 오브젝트")]
    public GameObject bossModel;

    [Tooltip("바닥에 깔릴 예고 장판 프리팹")]
    public GameObject teleportTelegraphPrefab;

    [Tooltip("보스가 사라질 때 제자리에 남길 이펙트 (선택)")]
    public GameObject disappearVfxPrefab;

    [Tooltip("장판이 터질 때(보스가 나타날 때) 터질 폭발 이펙트")]
    public GameObject appearExplosionVfxPrefab;

    public float teleportRadius = 4.0f;
    public float teleportDamage = 40f;

    [Tooltip("장판이 생성되고 터질 때까지의 총 시간")]
    public float teleportDuration = 1.5f;

    [Tooltip("터지기 몇 초 전부터 보스가 바닥으로 빨려 들어갈지 (DOTween 시간)")]
    public float disappearAnimTime = 0.3f;

    [Tooltip("나타날 때 커지면서 튀어나오는 시간 (DOTween 시간)")]
    public float appearAnimTime = 0.3f;

    [Header("Pattern 2: Kamikaze Arc (길막 폭탄 소환)")]
    public GameObject kamikazePrefab;
    public int kamikazeCount = 5;

    [Tooltip("머리 위 반원의 반지름 크기")]
    public float arcRadius = 3f;

    [Tooltip("반원의 중심점 높이 (보스 발바닥 기준)")]
    public float arcHeightOffset = 1.5f;

    private Transform cachedVfxContainer = null;
    private Collider bossCollider;

    // 모델링의 원래 크기와 위치를 기억해둘 변수
    private Vector3 originalModelScale;
    private Vector3 originalModelLocalPos;

    protected override void Start()
    {
        base.Start();
        bossCollider = GetComponent<Collider>();

        if (bossModel != null)
        {
            originalModelScale = bossModel.transform.localScale;
            originalModelLocalPos = bossModel.transform.localPosition;
        }
    }

    protected override void Attack()
    {
        if (isCastingPattern) return;
        isCastingPattern = true;

        if (Random.value > 0.5f)
        {
            StartCoroutine(TeleportStrikeRoutine()); // 이전의 순간이동 패턴
        }
        else
        {
            StartCoroutine(SpawnKamikazeArcRoutine()); // 신규 길막 폭탄 패턴
        }
    }

    private IEnumerator TeleportStrikeRoutine()
    {
        if (playerTarget == null || bossModel == null)
        {
            isCastingPattern = false;
            yield break;
        }

        Debug.Log("순간이동 강타 시전!");
        Transform vfxContainer = GetVfxContainer();

        // 1. 플레이어 위치 확보 및 바닥 보정
        Vector3 targetPosition = playerTarget.position;
        Vector3 spawnPosition = targetPosition;
        Vector3 rayStart = targetPosition + Vector3.up * raycastStartHeight;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
        {
            spawnPosition = hit.point;
        }

        // 2. 플레이어 발밑에 예고 장판(Telegraph) 깔기
        if (teleportTelegraphPrefab != null)
        {
            GameObject telegraphInstance = Instantiate(teleportTelegraphPrefab, spawnPosition, Quaternion.identity, vfxContainer);
            BossTelegraph script = telegraphInstance.GetComponent<BossTelegraph>();

            if (script != null)
            {
                // 장판이 터질 때 나타날 폭발 이펙트를 넘겨줍니다.
                script.explosionVfxPrefab = appearExplosionVfxPrefab;
                script.Initialize(teleportDamage, teleportRadius, teleportDuration, playerLayerMask);
            }
        }

        // 3. 보스가 사라지기 시작할 시간까지 대기
        float waitBeforeDisappear = teleportDuration - disappearAnimTime;
        if (waitBeforeDisappear > 0) yield return new WaitForSeconds(waitBeforeDisappear);

        // ==========================================
        // 4. [DOTween] 사라지는 연출 (크기 축소 + 땅으로 꺼짐)
        // ==========================================
        if (disappearVfxPrefab != null)
        {
            Instantiate(disappearVfxPrefab, transform.position, Quaternion.identity, vfxContainer);
        }

        // 무적 판정 (콜라이더 끄기)
        if (bossCollider != null) bossCollider.enabled = false;

        // InBack Ease를 쓰면 살짝 커졌다가 쏙! 하고 빨려 들어가는 찰진 연출이 나옵니다.
        bossModel.transform.DOScale(Vector3.zero, disappearAnimTime).SetEase(Ease.InBack);
        bossModel.transform.DOLocalMoveY(originalModelLocalPos.y - 3f, disappearAnimTime).SetEase(Ease.InBack);

        // 애니메이션이 끝날 때까지(= 장판이 터지는 정확한 타이밍) 대기
        yield return new WaitForSeconds(disappearAnimTime);

        // ==========================================
        // 5. [DOTween] 순간이동 및 나타나는 연출
        // ==========================================
        // 최상위 부모(보스 전체)를 목표 위치로 강제 이동!
        transform.position = spawnPosition;

        // 땅 밑에 있는 상태에서 크기를 0으로 맞춤 (나타날 준비)
        bossModel.transform.localPosition = new Vector3(originalModelLocalPos.x, originalModelLocalPos.y - 3f, originalModelLocalPos.z);
        bossModel.transform.localScale = Vector3.zero;

        // OutBack Ease를 쓰면 튀어 오르면서 살짝 튕기는 타격감 있는 연출이 나옵니다.
        bossModel.transform.DOScale(originalModelScale, appearAnimTime).SetEase(Ease.OutBack);
        bossModel.transform.DOLocalMoveY(originalModelLocalPos.y, appearAnimTime).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(appearAnimTime);

        // 6. 안전장치: 애니메이션 종료 후 콜라이더 및 상태 완벽 복구
        bossModel.transform.localScale = originalModelScale;
        bossModel.transform.localPosition = originalModelLocalPos;
        if (bossCollider != null) bossCollider.enabled = true;

        yield return new WaitForSeconds(0.5f); // 후딜레이
        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    private IEnumerator SpawnKamikazeArcRoutine()
    {
        if (playerTarget == null)
        {
            isCastingPattern = false;
            yield break;
        }

        Debug.Log("팬텀 보스: 길막 폭탄 소환!");
        Transform vfxContainer = GetVfxContainer();

        Vector3 centerPos = transform.position + Vector3.up * arcHeightOffset;

        // [수정됨] 소환된 폭탄들을 담아둘 리스트 선언 (누락되었던 부분)
        List<KamikazeProjectile> bombs = new List<KamikazeProjectile>();

        // 1. 보스 머리 위에 반원으로 소환 (기존 연출 유지)
        for (int i = 0; i < kamikazeCount; i++)
        {
            float t = kamikazeCount > 1 ? (float)i / (kamikazeCount - 1) : 0.5f;
            float angleDeg = Mathf.Lerp(180f, 0f, t);
            float angleRad = angleDeg * Mathf.Deg2Rad;

            float x = Mathf.Cos(angleRad) * arcRadius;
            float y = Mathf.Sin(angleRad) * arcRadius;

            Vector3 spawnOffset = (transform.right * x) + (transform.up * y);
            Vector3 spawnPos = centerPos + spawnOffset;

            if (kamikazePrefab != null)
            {
                GameObject projObj = Instantiate(kamikazePrefab, spawnPos, transform.rotation, vfxContainer);
                KamikazeProjectile proj = projObj.GetComponent<KamikazeProjectile>();

                if (proj != null)
                {
                    proj.Initialize(playerLayerMask);
                    bombs.Add(proj); // 리스트에 추가
                }
            }
            yield return new WaitForSeconds(0.1f);
        }

        // 2. 머리 위에 소환된 상태로 플레이어에게 위협을 주며 잠시 대기
        yield return new WaitForSeconds(0.5f);

        // 3. 플레이어 이동 경로 앞을 가로막는 일직선 좌표 계산 및 순간이동
        if (playerTarget != null)
        {
            // 플레이어가 바라보는 앞쪽 방향
            Vector3 playerForward = playerTarget.forward;

            // 앞쪽 방향과 위쪽 방향(Up)을 외적(Cross)하여 완벽한 '좌우 가로선' 방향을 구합니다.
            Vector3 lineDirection = Vector3.Cross(Vector3.up, playerForward).normalized;

            // 플레이어보다 약간(2.5m) 앞쪽을 기준점(가운데 폭탄 위치)으로 잡습니다.
            Vector3 wallCenterPos = playerTarget.position + playerForward * 8f;

            // 폭탄 간의 간격 (장판끼리 겹쳐서 도망칠 틈이 없도록 폭발 반지름의 1.5배 정도로 설정)
            float bombSpacing = 5f;

            for (int i = 0; i < bombs.Count; i++)
            {
                if (bombs[i] != null)
                {
                    // 5개 기준: -2, -1, 0, 1, 2 의 인덱스를 가집니다.
                    float offsetIndex = i - (bombs.Count - 1) / 2f;

                    // 중앙에서 좌우로 간격만큼 띄운 최종 목적지 좌표
                    Vector3 targetPos = wallCenterPos + lineDirection * (offsetIndex * bombSpacing);

                    // 각 폭탄에게 해당 위치로 순간이동해서 터지라고 명령!
                    bombs[i].TeleportAndFuse(targetPos);
                }
            }
        }

        // 폭탄이 터지는 시간 등을 고려한 후딜레이
        yield return new WaitForSeconds(1.5f);
        isCastingPattern = false;
        currentAttackCooldown = attackCooltime;
    }

    private Transform GetVfxContainer()
    {
        if (cachedVfxContainer != null) return cachedVfxContainer;

        if (EnemyManager.Instance != null && EnemyManager.Instance.FxParent != null)
        {
            cachedVfxContainer = EnemyManager.Instance.FxParent;
        }
        return cachedVfxContainer;
    }

    // [추가됨] 보스가 DOTween 애니메이션 도중 죽었을 때 에러(메모리 릭) 방지
    protected override void Die()
    {
        if (bossModel != null)
        {
            bossModel.transform.DOKill();
        }
        base.Die();
    }
}