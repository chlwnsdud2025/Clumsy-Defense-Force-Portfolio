using UnityEngine;

[DisallowMultipleComponent]
public class PlayerExperiencePickupCollector : MonoBehaviour
{
    private const int OverlapBufferSize = 64;
    private const float FallbackPickupRange = 10f;

    [SerializeField] private Transform _origin;
    [SerializeField] private LayerMask _experienceMask;

    [Tooltip("자석 범위 안에 있는 경험치 오브가 플레이어 방향으로 이동하는 속도(유닛/초)")]
    [SerializeField][Min(0.01f)] private float _magnetPullSpeed = 10f;

    [Tooltip("이 거리 이하로 가까워지면 경험치 흡수")]
    [SerializeField][Min(0.01f)] private float _contactAbsorbRadius = 0.45f;

    [Tooltip("Scene 뷰에서 흡수 반경 기준 표시 여부")]
    [SerializeField] private bool _showPickupRangeGizmo = true;

    private Collider[] _overlapBuffer;
    private PlayerRuntime _runtime;

    private void Awake()
    {
        _overlapBuffer = new Collider[OverlapBufferSize];
    }

    public void BindRuntime(PlayerRuntime runtime)
    {
        _runtime = runtime;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        float magnetRange = FallbackPickupRange * GetPickupRangeMultiplier();
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
                AddExperience(amount);

                if (RunManager.TryGetInstance(out RunManager runManager) &&
                    runManager.RunContext != null &&
                    !runManager.RunContext.ShouldUpdateGameplay)
                {
                    break;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!_showPickupRangeGizmo)
            return;

        float range = FallbackPickupRange;
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

    private void AddExperience(int amount)
    {
        if (amount <= 0)
            return;

        if (!TryGetPlayerRuntime(out PlayerRuntime playerRuntime))
            return;

        if (playerRuntime.Health.CurrentHp <= 0f)
            return;

        float expMultiplier = Mathf.Max(0f, playerRuntime.Stats.GetValue(EStatType.ExpGainMultiplier));
        int finalAmount = Mathf.Max(0, Mathf.RoundToInt(amount * expMultiplier));
        if (finalAmount <= 0)
            return;

        if (!CanAddExperience())
            return;

        playerRuntime.Level.AddExp(finalAmount);
    }

    private bool CanAddExperience()
    {
        if (!RunManager.TryGetInstance(out RunManager runManager))
            return false;

        RunContext runContext = runManager.RunContext;
        return runContext != null && runContext.ShouldUpdateGameplay;
    }

    private float GetPickupRangeMultiplier()
    {
        if (!TryGetPlayerRuntime(out PlayerRuntime playerRuntime))
            return 1f;

        return Mathf.Max(0f, playerRuntime.Stats.GetValue(EStatType.PickupRangeMultiplier));
    }

    private bool TryGetPlayerRuntime(out PlayerRuntime playerRuntime)
    {
        playerRuntime = _runtime;
        if (playerRuntime != null)
            return true;

        if (!RunManager.TryGetInstance(out RunManager runManager))
            return false;

        RunContext runContext = runManager.RunContext;
        if (runContext == null)
            return false;

        playerRuntime = runContext.Player;
        return playerRuntime != null;
    }
}
