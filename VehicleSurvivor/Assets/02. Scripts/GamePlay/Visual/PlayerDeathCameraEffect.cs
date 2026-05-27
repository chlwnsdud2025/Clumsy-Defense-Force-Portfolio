using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerDeathCameraEffect : MonoBehaviour
{
    [SerializeField] private CinemachineThirdPersonFollow thirdPersonFollow;

    [Header("Distance")]
    [Tooltip("사망 연출 종료 거리 = 시작 거리 × 이 비율 (0.6이면 40% 가까워짐)")]
    [SerializeField, Range(0.1f, 1f)] private float targetDistanceRatio = 0.6f;
    [SerializeField] private float blendDuration = 0.35f;

    private float baselineCameraDistance;

    private void Awake()
    {
        CaptureBaselineDistance();
    }

    public void ResetForNewRun()
    {
        if (!TryResolveThirdPersonFollow())
            return;

        if (baselineCameraDistance <= 0f)
            CaptureBaselineDistance();

        thirdPersonFollow.CameraDistance = baselineCameraDistance;
    }

    public UniTask PlayAsync()
    {
        if (!TryResolveThirdPersonFollow())
        {
            Debug.LogWarning("[PlayerDeathCameraEffect] CinemachineThirdPersonFollow를 찾을 수 없어 줌 연출을 건너뜁니다.", this);
            return UniTask.CompletedTask;
        }

        return PlayDistanceZoomAsync();
    }

    private void CaptureBaselineDistance()
    {
        if (!TryResolveThirdPersonFollow())
            return;

        baselineCameraDistance = thirdPersonFollow.CameraDistance;
    }

    private bool TryResolveThirdPersonFollow()
    {
        if (thirdPersonFollow != null)
            return true;

        thirdPersonFollow = GetComponentInChildren<CinemachineThirdPersonFollow>(true);
        return thirdPersonFollow != null;
    }

    private async UniTask PlayDistanceZoomAsync()
    {
        float startDistance = thirdPersonFollow.CameraDistance;
        float targetDistance = startDistance * targetDistanceRatio;
        float elapsed = 0f;

        while (elapsed < blendDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = blendDuration > 0f ? Mathf.Clamp01(elapsed / blendDuration) : 1f;
            thirdPersonFollow.CameraDistance = Mathf.Lerp(startDistance, targetDistance, t);
            await UniTask.Yield(PlayerLoopTiming.Update, destroyCancellationToken);
        }

        thirdPersonFollow.CameraDistance = targetDistance;
    }
}
