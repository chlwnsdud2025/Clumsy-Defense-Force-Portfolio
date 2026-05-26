using Unity.Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerHitCameraShake : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("피격 임펄스를 발생시킬 소스")]
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Tooltip("카메라 쉐이크를 수신할 카메라. 비어 있으면 자식 Camera를 찾습니다.")]
    [SerializeField] private Camera targetCamera;

    [Header("Impulse")]
    [SerializeField] private float impulseForce = 0.25f;
    [SerializeField] private float impulseDuration = 0.12f;

    [Header("Listener")]
    [SerializeField] private float listenerGain = 0.35f;

    [Header("Cooldown")]
    [SerializeField] private float shakeCooldown = 0.1f;

    private float lastShakeTime = float.NegativeInfinity;
    private bool isInitialized;

    private CinemachineImpulseSource ImpulseSource
    {
        get
        {
            if (impulseSource == null)
                ResolveImpulseSource();
            return impulseSource;
        }
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    public void TriggerShake()
    {
        if (!EnsureInitialized())
            return;

        if (Time.time - lastShakeTime < shakeCooldown)
            return;

        lastShakeTime = Time.time;
        ImpulseSource.GenerateImpulseWithForce(impulseForce);
    }

    private bool EnsureInitialized()
    {
        if (isInitialized)
            return ImpulseSource != null;

        if (ImpulseSource == null)
            return false;

        ConfigureImpulseSource(ImpulseSource);
        EnsureListener();

        isInitialized = true;
        return true;
    }

    private void ResolveImpulseSource()
    {
        if (impulseSource != null)
            return;

        impulseSource = GetComponent<CinemachineImpulseSource>();
        if (impulseSource == null)
            impulseSource = gameObject.GetOrAddComponent<CinemachineImpulseSource>();
    }

    private void ResolveTargetCamera()
    {
        if (targetCamera != null)
            return;

        targetCamera = GetComponentInChildren<Camera>(true);
    }

    private void ConfigureImpulseSource(CinemachineImpulseSource source)
    {
        source.DefaultVelocity = new Vector3(0f, -1f, 0f);

        CinemachineImpulseDefinition definition = source.ImpulseDefinition;
        definition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
        definition.ImpulseDuration = impulseDuration;
        definition.PropagationSpeed = 343f;
    }

    private void EnsureListener()
    {
        ResolveTargetCamera();
        if (targetCamera == null)
            return;

        CinemachineExternalImpulseListener listener =
            targetCamera.GetOrAddComponent<CinemachineExternalImpulseListener>();

        listener.ChannelMask = ImpulseSource.ImpulseDefinition.ImpulseChannel;
        listener.Gain = listenerGain;
        listener.UseLocalSpace = true;
    }
}
