using System.Collections.Generic;
using UnityEngine;

public class HomingProjectile : MonoBehaviour
{
    [Header("Homing (Gliding)")]
    [Tooltip("숫자가 작을수록 둔하게(크게 돌고), 클수록 날카롭게 꺾입니다. (추천: 2 ~ 4)")]
    public float rotateSpeed = 3f;

    [Header("Visual Model Correction")]
    [Tooltip("비행기 모델링의 코(앞)가 X축을 향하고 있다면 90, Z축이라면 0을 넣으세요.")]
    public float modelYRotationOffset = 0f;

    [Header("Piercing & Life")]
    [Tooltip("종이비행기가 파괴되지 않고 맵을 날아다니는 수명 (초)")]
    public float lifeTime = 4f;
    [Tooltip("같은 적을 관통한 뒤 다시 타격하기까지 걸리는 쿨타임 (초)")]
    public float hitCooldown = 0.5f;

    private DamageInfo damageInfo;
    private float speed;

    private Vector3 moveDirection;
    private float aliveTimer; // 이동 거리가 아닌 생존 시간을 측정합니다.

    private Transform target;
    private Collider targetCollider;
    private float detectRadius;

    // [추가] 동일 대상 다단히트를 방지하기 위한 타격 타이머 딕셔너리
    private readonly Dictionary<IDamageable, float> hitTimers = new Dictionary<IDamageable, float>();

    public void Initialize(
        DamageInfo newDamageInfo,
        float newSpeed,
        float newMaxTravelDistance, // HomingWeapon 호환성을 위해 파라미터는 남겨둡니다.
        Vector3 newMoveDirection,
        Transform newTarget,
        float newDetectRadius,
        int newPierceCount // 관통 횟수도 이제 무제한이므로 내부에서 사용하지 않습니다.
    )
    {
        damageInfo = newDamageInfo;
        speed = newSpeed;

        moveDirection = newMoveDirection.normalized;
        aliveTimer = 0f;
        hitTimers.Clear(); // 오브젝트 풀링 재사용 시 딕셔너리 초기화

        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection) * Quaternion.Euler(0, modelYRotationOffset, 0);
        }

        SetTarget(newTarget);
        detectRadius = newDetectRadius;
    }

    private void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            targetCollider = target.GetComponentInChildren<Collider>();
        }
    }

    private void Update()
    {
        // 1. 타겟 유효성 체크 및 재탐색
        if (target != null && !target.gameObject.activeInHierarchy)
        {
            target = null;
            targetCollider = null;
        }

        if (target == null)
        {
            UpdateTarget();
        }

        // 2. 유도 로직
        if (target != null)
        {
            Vector3 aimPosition = targetCollider != null
                ? targetCollider.bounds.center
                : target.position + Vector3.up * 0.5f;

            Vector3 targetDirection = (aimPosition - transform.position);

            // [수정] 충돌체계가 바뀌었으므로 타겟 중심에 도달해도 즉시 파괴하지 않고 자연스럽게 지나가도록 둡니다.

            moveDirection = Vector3.RotateTowards(
                moveDirection,
                targetDirection.normalized,
                rotateSpeed * Time.deltaTime,
                0f
            ).normalized;
        }

        // 3. 이동 및 회전 처리
        float moveStep = speed * Time.deltaTime;
        transform.position += moveDirection * moveStep;

        if (moveDirection != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = lookRotation * Quaternion.Euler(0, modelYRotationOffset, 0);
        }

        // 4. [핵심 수정] 이동 거리(Distance)가 아닌 수명(Time)으로 파괴 체크
        aliveTimer += Time.deltaTime;
        if (aliveTimer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateTarget()
    {
        if (target != null)
            return;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectRadius
        );

        float closestDistance = Mathf.Infinity;
        Collider closestCollider = null;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
                continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null)
                continue;

            float distance = (hit.transform.position - transform.position).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCollider = hit;
            }
        }

        if (closestCollider != null)
        {
            SetTarget(closestCollider.transform);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerLayer(other))
            return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return;

        // [추가] 재타격 쿨타임 체크 (다단히트 방지)
        if (hitTimers.ContainsKey(damageable))
        {
            if (Time.time < hitTimers[damageable])
                return; // 아직 쿨타임이 지나지 않았다면 데미지를 주지 않고 무시
        }

        // 타격 시간 갱신
        hitTimers[damageable] = Time.time + hitCooldown;

        damageable.TakeDamage(damageInfo);
        Debug.Log($"종이비행기(관통) 데미지 들어감: {damageInfo.Amount}");

        // [핵심 수정] 기존의 관통 횟수(pierceCount) 차감 및 Destroy(gameObject) 파괴 로직을 전부 삭제하여 무한히 뚫고 지나가게 합니다!
    }

    private bool IsPlayerLayer(Collider other)
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer == -1)
            return false;

        if (other.gameObject.layer == playerLayer)
            return true;

        Transform root = other.transform.root;
        return root.gameObject.layer == playerLayer;
    }
}