using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
[RequireComponent(typeof(PlayerInput))]
#endif
public class PlayerController : MonoBehaviour
{
    // 애니메이터 파라미터 이름(Animator Controller와 반드시 일치)
    private const string AnimParamSpeed = "Speed";
    private const string AnimParamGrounded = "Grounded";
    private const string AnimParamJump = "Jump";
    private const string AnimParamFreeFall = "FreeFall";
    private const string AnimParamMotionSpeed = "MotionSpeed";

    private const float _threshold = 0.01f;

    [Header("플레이어")]
    [Tooltip("걷기 이동 속도")]
    public float MoveSpeed = 5f;

    [Tooltip("달리기 이동 속도")]
    public float SprintSpeed = 10f;

    [Tooltip("이동 방향을 바라볼 때 회전이 따라잡는 속도")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("가속·감속 반응 속도")]
    public float SpeedChangeRate = 10.0f;

    [Space(10)]
    [Tooltip("점프 높이")]
    public float JumpHeight = 1.2f;

    [Tooltip("중력(엔진 기본값은 -9.81)")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("다시 점프하기 전 대기 시간(0이면 즉시)")]
    public float JumpTimeout = 0.30f;

    [Tooltip("낙하 상태로 넘어가기 전 대기 시간(계단 등에 유리)")]
    public float FallTimeout = 0.15f;

    [Header("지면 판정")]
    [Tooltip("지면에 닿았는지 여부(CharacterController 내장 판정과 별개)")]
    public bool Grounded = true;

    [Tooltip("울퉁불퉁한 지형을 위한 검사 위치 오프셋")]
    public float GroundedOffset = -0.14f;

    [Tooltip("지면 검사 구체 반경(CharacterController 반경과 맞출 것)")]
    public float GroundedRadius = 0.28f;

    [Tooltip("지면으로 취급할 레이어")]
    public LayerMask GroundLayers;

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

    // 시네머신
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    // 플레이어
    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;

    // 점프·낙하 타이머
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    // 애니메이션 파라미터 해시 ID
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif
    private Animator _animator;
    private CharacterController _controller;
    private PlayerInputs _input;

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

    private void Start()
    {
        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        _hasAnimator = TryGetComponent(out _animator);
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<PlayerInputs>();
#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError("Starter Assets 의존성이 없습니다. 메뉴 Tools/Starter Assets/Reinstall Dependencies로 복구하세요.");
#endif

        AssignAnimationIDs();

        // 시작 시 점프·낙하 타이머 초기화
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    private void Update()
    {
        _hasAnimator = TryGetComponent(out _animator);

        JumpAndGravity();
        GroundedCheck();
        Move();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash(AnimParamSpeed);
        _animIDGrounded = Animator.StringToHash(AnimParamGrounded);
        _animIDJump = Animator.StringToHash(AnimParamJump);
        _animIDFreeFall = Animator.StringToHash(AnimParamFreeFall);
        _animIDMotionSpeed = Animator.StringToHash(AnimParamMotionSpeed);
    }

    private void CameraRotation()
    {
        // 입력이 있고 카메라가 잠기지 않았을 때만
        if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            // 마우스 입력에는 deltaTime을 곱하지 않음
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
            float lookSensitivity = IsCurrentDeviceMouse ? MouseLookSensitivity : GamepadLookSensitivity;

            _cinemachineTargetYaw += _input.look.x * lookSensitivity * deltaTimeMultiplier;
            _cinemachineTargetPitch += _input.look.y * lookSensitivity * deltaTimeMultiplier;
        }

        // Yaw/Pitch를 클램프해 시야가 지나치게 돌지 않게 함
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // 시네머신이 이 타깃 회전을 추적함
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw, 0.0f);
    }

    private void Move()
    {
        if (_mainCamera == null)
            return;

        // 이동·달리기 입력에 따른 목표 속도
        float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

        // 단순 가속·감속(교체·튜닝이 쉬운 형태)
        // Vector2 == 는 근사 비교라 magnitude보다 가볍고 부동소수 오차에 덜 취약함
        // 입력이 없으면 목표 속도 0
        if (_input.move == Vector2.zero)
            targetSpeed = 0.0f;

        // 수평 속력 크기
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        // 목표 속도까지 가속 또는 감속
        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            // Lerp로 속도 변화가 덜 각지게 보이게 함
            // Lerp의 t는 이미 클램프되어 있어 별도 속도 클램프 불필요
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                Time.deltaTime * SpeedChangeRate);

            // 속도를 소수 셋째 자리로 반올림
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        // 입력 방향 정규화
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        // Vector2 != 도 근사 비교
        // 이동 중이면 카메라 기준으로 몸통 회전
        if (_input.move != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                              _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                RotationSmoothTime);

            // 카메라 기준 입력 방향을 바라봄
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }


        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        // 캐릭터 이동 적용
        _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                         new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        // 애니메이터가 있으면 동기화
        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            // 낙하 타이머 초기화
            _fallTimeoutDelta = FallTimeout;

            // 애니메이터가 있으면 동기화
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            // 지면에 닿았을 때 수직 속도가 무한히 떨어지지 않게 바닥에 붙임
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // 점프
            if (_input.jump && _jumpTimeoutDelta <= 0.0f)
            {
                // 목표 높이 H일 때 필요한 초기 수직 속도: √(H · -2 · G)
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                // 애니메이터가 있으면 동기화
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
            }

            // 연속 점프 쿨다운
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // 점프 쿨다운 타이머 초기화
            _jumpTimeoutDelta = JumpTimeout;

            // 공중에 머문 뒤 낙하 상태로 넘어가기까지 대기
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                // 애니메이터가 있으면 동기화
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                }
            }

            // 공중에서는 점프 입력 무시
            _input.jump = false;
        }

        _verticalVelocity += Gravity * Time.deltaTime;
    }

    private void GroundedCheck()
    {
        // 오프셋을 반영한 구체 중심 위치
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);

        // 애니메이터가 있으면 동기화
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, Grounded);
        }
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
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // 선택 시 지면 검사 구체 위치·반경 표시
        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
            GroundedRadius);
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            // TODO 발걸음 사운드 재생
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            // TODO 착지 사운드 재생
        }
    }
}
