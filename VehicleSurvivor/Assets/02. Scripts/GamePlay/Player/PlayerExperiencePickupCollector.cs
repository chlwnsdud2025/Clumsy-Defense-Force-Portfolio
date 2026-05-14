using UnityEngine;

[DisallowMultipleComponent]
public class PlayerExperiencePickupCollector : MonoBehaviour
{
    private const int OverlapBufferSize = 64;
    private const float FallbackPickupRange = 10f;

    [SerializeField] private Transform _origin;
    [SerializeField] private LayerMask _experienceMask;

    [Tooltip("자석 범위 안에서 경험치 오브가 플레이어 방향으로 이동하는 속도(유닛/초)")]
    [SerializeField][Min(0.01f)] private float _magnetPullSpeed = 10f;

    [Tooltip("이 거리 이하로 가까워지면 경험치 획득")]
    [SerializeField][Min(0.01f)] private float _contactAbsorbRadius = 0.45f;

    [Tooltip("Scene 뷰에서 획득 반경 기즈모(와이어 구체) 표시 여부")]
    [SerializeField] private bool _showPickupRangeGizmo = true;

    private Collider[] _overlapBuffer;

    private void Awake()
    {
        _overlapBuffer = new Collider[OverlapBufferSize];
    }

    private void Update()
    {
        float magnetRange = FallbackPickupRange;
        if (magnetRange <= 0f)
            return;

        Vector3 center = _origin != null ? _origin.position : transform.position;
        int count = Physics.OverlapSphereNonAlloc(
            center,
            magnetRange,
            _overlapBuffer,
            _experienceMask,
            QueryTriggerInteraction.Collide);

        float pullStep = _magnetPullSpeed * Time.deltaTime;
        float absorbSqr = _contactAbsorbRadius * _contactAbsorbRadius;

        for (int i = 0; i < count; i++)
        {
            Collider collider = _overlapBuffer[i];
            if (collider == null)
                continue;

            if (!collider.TryGetComponent(out ExpOrb pickup))
                pickup = collider.GetComponentInParent<ExpOrb>();

            if (pickup == null)
                continue;

            pickup.ApplyPullTowards(center, pullStep);

            if ((pickup.transform.position - center).sqrMagnitude > absorbSqr)
                continue;

            if (pickup.TryConsume(out int amount))
            {
                // TODO 경험치 획득 처리
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!_showPickupRangeGizmo)
            return;

        float range = FallbackPickupRange;
;
        if (range <= 0f)
            return;

        Vector3 center = _origin != null ? _origin.position : transform.position;
        Gizmos.color = new Color(0.35f, 0.85f, 0.45f, 1f);
        Gizmos.DrawWireSphere(center, range);

        if (_contactAbsorbRadius > 0f)
        {
            Gizmos.color = new Color(1f, 0.65f, 0.2f, 1f);
            Gizmos.DrawWireSphere(center, _contactAbsorbRadius);
        }
    }
}
