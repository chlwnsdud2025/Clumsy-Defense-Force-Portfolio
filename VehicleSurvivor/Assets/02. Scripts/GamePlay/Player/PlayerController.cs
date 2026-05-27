using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(PlayerMotor))]
#if ENABLE_INPUT_SYSTEM
[RequireComponent(typeof(PlayerInput))]
#endif
public class PlayerController : MonoBehaviour
{
    private const string AnimParamSpeed = "Speed";
    private const string AnimParamGrounded = "Grounded";
    private const string AnimParamJump = "Jump";
    private const string AnimParamFreeFall = "FreeFall";
    private const string AnimParamMotionSpeed = "MotionSpeed";
    private const string AnimParamDie = "Die";

    private const float Threshold = 0.01f;
    private const float BlendMaxSpeed = 6f;

    [Header("애니메이션")]
    [Tooltip("BlendTree Run 임계값(6)에 맞춘 달리기 클립 기준 속도")]
    [SerializeField]
    private float _runAnimReferenceSpeed = 6f;

    [Header("사망 연출")]
    [SerializeField] private PlayerDeathCameraEffect _deathCameraEffect;
    [SerializeField] private PlayerHitCameraShake _hitCameraShake;

    [Header("카메라")]
    [Tooltip("이동 방향 기준으로 사용합니다.")]
    [SerializeField]
    private GameObject _mainCamera;

    [Header("시네머신")]
    [Tooltip("가상 카메라가 추적할 타깃 오브젝트")]
    public GameObject CinemachineCameraTarget;

    [Tooltip("카메라를 위로 기울일 수 있는 각도 한계(도)")]
    public float TopClamp = 70.0f;

    [Tooltip("카메라를 아래로 기울일 수 있는 각도 한계(도)")]
    public float BottomClamp = -30.0f;

    [Tooltip("카메라 각도 보정값(잠금 시 미세 조정에 사용)")]
    public float CameraAngleOverride = 0.0f;

    [Tooltip("마우스 시야 민감도 배율")]
    public float MouseLookSensitivity = 3.0f;

    [Tooltip("게임패드 시야 민감도 배율")]
    public float GamepadLookSensitivity = 120.0f;

    [Tooltip("카메라 회전을 잠글지 여부")]
    public bool LockCameraPosition = false;

    public bool IsDeathPresentationActive { get; private set; }

    public PlayerMotor Motor { get; private set; }

    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif
    private Animator _animator;
    private PlayerInputs _input;
    private PlayerRuntime _runtime;
    private bool _hasAnimator;

    private bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return _playerInput.currentControlScheme == "KeyboardMouse";
#else
            return false;
#endif
        }
    }

    private void Awake()
    {
        ResolveMainCamera();
        Motor = GetComponent<PlayerMotor>();

        if (_deathCameraEffect == null)
            _deathCameraEffect = GetComponentInParent<PlayerDeathCameraEffect>();

        if (_hitCameraShake == null)
            _hitCameraShake = GetComponentInParent<PlayerHitCameraShake>();
    }

    private void Start()
    {
        if (CinemachineCameraTarget != null)
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        _hasAnimator = TryGetComponent(out _animator);
        _input = GetComponent<PlayerInputs>();
#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<PlayerInput>();
#else
        Debug.LogError("Starter Assets 의존성이 없습니다. 메뉴 Tools/Starter Assets/Reinstall Dependencies로 복구하세요.");
#endif

        AssignAnimationIDs();
    }

    public void BindRuntime(PlayerRuntime runtime)
    {
        _runtime = runtime;
        if (Motor != null)
            Motor.BindRuntime(runtime);

        if (runtime != null)
            ResetForNewRun();
    }

    private void ResetForNewRun()
    {
        SetDeathPresentationActive(false);
        LockCameraPosition = false;
        _deathCameraEffect?.ResetForNewRun();
        _hitCameraShake?.ResetForNewRun();
    }

    public void SetDeathPresentationActive(bool active)
    {
        IsDeathPresentationActive = active;
        if (active)
            LockCameraPosition = true;

        Motor?.SetDeathPresentationActive(active);
    }

    public async UniTaskVoid PlayDeathPresentationAsync(Action onComplete)
    {
        if (IsDeathPresentationActive)
            return;

        AnimatorUpdateMode previousUpdateMode = AnimatorUpdateMode.Normal;

        if (_hasAnimator)
        {
            previousUpdateMode = _animator.updateMode;
            _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        SetDeathPresentationActive(true);

        try
        {
            if (_hasAnimator)
            {
                _animator.ResetTrigger(AnimParamDie);
                _animator.SetTrigger(AnimParamDie);
            }

            if (_deathCameraEffect != null)
                await _deathCameraEffect.PlayAsync();

            onComplete?.Invoke();
        }
        catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            if (_hasAnimator)
                _animator.updateMode = previousUpdateMode;
        }
    }

    private void Update()
    {
        if (IsDeathPresentationActive)
            return;

        if (_mainCamera == null || Motor == null || _input == null)
            return;

        float cameraYaw = _mainCamera.transform.eulerAngles.y;
        Motor.SetInputs(_input.move, _input.jump, _input.analogMovement, cameraYaw);
        _input.jump = false;

        UpdateAnimator();
    }

    private void LateUpdate()
    {
        if (IsDeathPresentationActive)
            return;

        CameraRotation();
    }

    private void UpdateAnimator()
    {
        if (!_hasAnimator || Motor == null)
            return;

        float baseMoveSpeed = Mathf.Max(Motor.MoveSpeed, 0.01f);
        float animBlendSpeed = Mathf.Clamp(Motor.AnimationBlend / baseMoveSpeed * BlendMaxSpeed, 0f, BlendMaxSpeed);
        float runRef = Mathf.Max(_runAnimReferenceSpeed, 0.01f);
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;
        float motionSpeed = (Motor.AnimationBlend / runRef) * inputMagnitude;

        _animator.SetFloat(_animIDSpeed, animBlendSpeed);
        _animator.SetFloat(_animIDMotionSpeed, motionSpeed);
        _animator.SetBool(_animIDGrounded, Motor.Grounded);
        _animator.SetBool(_animIDJump, Motor.JumpAnimActive);
        _animator.SetBool(_animIDFreeFall, Motor.FreeFallAnimActive);
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash(AnimParamSpeed);
        _animIDGrounded = Animator.StringToHash(AnimParamGrounded);
        _animIDJump = Animator.StringToHash(AnimParamJump);
        _animIDFreeFall = Animator.StringToHash(AnimParamFreeFall);
        _animIDMotionSpeed = Animator.StringToHash(AnimParamMotionSpeed);
    }

    private void ResolveMainCamera()
    {
        if (_mainCamera != null)
            return;

        GameObject tagged = GameObject.FindGameObjectWithTag("MainCamera");
        if (tagged != null)
            _mainCamera = tagged;

        if (_mainCamera == null)
            Debug.LogWarning("[PlayerController] Main Camera가 인스펙터에 할당되지 않았고 MainCamera 태그 오브젝트도 없습니다.", this);
    }

    private void CameraRotation()
    {
        if (_input == null || CinemachineCameraTarget == null)
            return;

        if (_input.look.sqrMagnitude >= Threshold && !LockCameraPosition)
        {
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
            float lookSensitivity = IsCurrentDeviceMouse ? MouseLookSensitivity : GamepadLookSensitivity;

            _cinemachineTargetYaw += _input.look.x * lookSensitivity * deltaTimeMultiplier;
            _cinemachineTargetPitch += _input.look.y * lookSensitivity * deltaTimeMultiplier;
        }

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
            _cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw,
            0.0f);
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        Motor?.Teleport(position, rotation);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnValidate()
    {
        MouseLookSensitivity = Mathf.Max(0f, MouseLookSensitivity);
        GamepadLookSensitivity = Mathf.Max(0f, GamepadLookSensitivity);
        _runAnimReferenceSpeed = Mathf.Max(0.01f, _runAnimReferenceSpeed);
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
            SoundPlayback.PlaySfx(ESoundId.SFX_Player_Footstep);
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
            SoundPlayback.PlaySfx(ESoundId.SFX_Player_Land);
    }
}
