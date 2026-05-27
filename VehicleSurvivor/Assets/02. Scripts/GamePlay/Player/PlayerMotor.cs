using KinematicCharacterController;
using UnityEngine;

[RequireComponent(typeof(KinematicCharacterMotor))]
public class PlayerMotor : MonoBehaviour, ICharacterController
{
    public KinematicCharacterMotor Motor;

    [Header("Movement")]
    public float MoveSpeed = 5f;
    public float SpeedChangeRate = 10f;
    public float RotationSmoothTime = 0.12f;
    public float AirAccelerationSpeed = 10f;
    public float AirDrag = 0.1f;

    [Header("Jump")]
    public float JumpHeight = 1.2f;
    public float Gravity = -15f;
    public float JumpTimeout = 0.3f;
    public float FallTimeout = 0.15f;
    public float GroundedCoyoteTime = 0.15f;
    [Tooltip("착지 직전에만 점프 입력을 유지합니다. 이 시간을 넘기면 공중에서 누른 점프는 무시됩니다.")]
    public float JumpPreGroundingGraceTime = 0.15f;

    [Header("Debug")]
    [SerializeField, ReadOnly] private bool isStableOnGround;
    [SerializeField, ReadOnly] private bool foundAnyGround;
    [SerializeField, ReadOnly] private bool grounded;

    public bool Grounded
    {
        get => grounded;
        private set => grounded = value;
    }

    public bool PhysicsGrounded { get; private set; }
    public float AnimationBlend { get; private set; }
    public float CurrentMoveSpeed { get; private set; }
    public bool JumpAnimActive { get; private set; }
    public bool FreeFallAnimActive { get; private set; }

    private Vector3 _moveInputVector;
    private Vector3 _internalVelocityAdd;
    private Vector3 _externalImpact;
    private Vector3 _conveyorVelocity;

    private bool _jumpRequested;
    private bool _jumpConsumed;
    private bool _jumpedThisFrame;
    private float _timeSinceJumpRequested = Mathf.Infinity;
    private float _timeSinceLastAbleToJump;
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;
    private float _ungroundedTimer;

    private bool _windActive;
    private float _windLiftSpeed;

    private PlayerRuntime _runtime;
    private float _currentScalarSpeed;
    private float _inputMagnitude = 1f;

    // ==========================================
    // [핵심 추가] 외부 런치(강제 점프/튕김) 상태 변수
    // ==========================================
    private bool _hasLaunchRequest;
    private Vector3 _launchVelocity;
    private bool _resetVerticalVelocity;
    private bool _resetHorizontalVelocity;
    private bool _isDeathPresentation;

    private void Awake()
    {
        if (Motor == null) Motor = GetComponent<KinematicCharacterMotor>();
        Motor.CharacterController = this;

        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    private void OnValidate()
    {
        MoveSpeed = Mathf.Max(0f, MoveSpeed);
        SpeedChangeRate = Mathf.Max(0.01f, SpeedChangeRate);
        RotationSmoothTime = Mathf.Max(0.01f, RotationSmoothTime);
        JumpTimeout = Mathf.Max(0f, JumpTimeout);
        FallTimeout = Mathf.Max(0f, FallTimeout);
        GroundedCoyoteTime = Mathf.Max(0f, GroundedCoyoteTime);
        JumpPreGroundingGraceTime = Mathf.Max(0f, JumpPreGroundingGraceTime);
    }

    public void BindRuntime(PlayerRuntime runtime) { _runtime = runtime; }

    public void SetInputs(Vector2 move, bool jumpPressed, bool analogMovement, float cameraYawDegrees)
    {
        Vector3 moveInput = Vector3.ClampMagnitude(new Vector3(move.x, 0f, move.y), 1f);
        _inputMagnitude = analogMovement ? moveInput.magnitude : 1f;

        float yawRad = cameraYawDegrees * Mathf.Deg2Rad;
        Quaternion cameraPlanarRotation = Quaternion.Euler(0f, cameraYawDegrees, 0f);
        _moveInputVector = cameraPlanarRotation * moveInput;

        if (jumpPressed)
        {
            _timeSinceJumpRequested = 0f;
            _jumpRequested = true;
        }
    }

    public void AddVelocity(Vector3 velocity) { _internalVelocityAdd += velocity; }
    public void SetConveyorVelocity(Vector3 velocity) { _conveyorVelocity = velocity; }

    // ==========================================
    // [핵심 수정] 속도 초기화(Reset) 플래그 추가!
    // ==========================================
    public void LaunchJump(float targetHeight, float horizontalForce, Vector3 direction, bool resetYVelocity = true, bool resetXZVelocity = false)
    {
        _externalImpact = Vector3.zero; // 외부 미끄러짐 방지

        // 중력이 음수이므로 절댓값을 씌워서 정확히 계산
        float jumpSpeed = Mathf.Sqrt(Mathf.Abs(targetHeight * -2f * Gravity));

        Vector3 verticalLaunch = Motor.CharacterUp * jumpSpeed;
        Vector3 horizontalLaunch = Vector3.ProjectOnPlane(direction.normalized * horizontalForce, Motor.CharacterUp);

        _launchVelocity = verticalLaunch + horizontalLaunch;
        _resetVerticalVelocity = resetYVelocity;
        _resetHorizontalVelocity = resetXZVelocity;
        _hasLaunchRequest = true; // 다음 UpdateVelocity 프레임에 적용 예약

        Motor.ForceUnground();
    }

    public void SetWindForce(float liftSpeed) { _windLiftSpeed = liftSpeed; _windActive = true; }
    public void RemoveWindForce() { _windLiftSpeed = 0f; _windActive = false; }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        Motor.SetPosition(position);
        Motor.SetRotation(rotation);
        HaltMovement();
    }

    public void SetDeathPresentationActive(bool active)
    {
        _isDeathPresentation = active;
        if (active)
            HaltMovement();
    }

    private void HaltMovement()
    {
        _moveInputVector = Vector3.zero;
        _currentScalarSpeed = 0f;
        AnimationBlend = 0f;
        _internalVelocityAdd = Vector3.zero;
        _externalImpact = Vector3.zero;
        _conveyorVelocity = Vector3.zero;
        _hasLaunchRequest = false;
        _launchVelocity = Vector3.zero;

        if (Motor == null)
            return;

        Motor.BaseVelocity = Vector3.zero;
    }

    public void BeforeCharacterUpdate(float deltaTime) { }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (_isDeathPresentation)
            return;

        if (_moveInputVector.sqrMagnitude <= 0.0001f) return;

        Vector3 lookDirection = Vector3.ProjectOnPlane(_moveInputVector, Motor.CharacterUp).normalized;
        if (lookDirection.sqrMagnitude <= 0.0001f) return;

        float sharpness = 1f / RotationSmoothTime;
        Vector3 smoothedDirection = Vector3.Slerp(Motor.CharacterForward, lookDirection, 1f - Mathf.Exp(-sharpness * deltaTime)).normalized;
        currentRotation = Quaternion.LookRotation(smoothedDirection, Motor.CharacterUp);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (_isDeathPresentation)
        {
            currentVelocity = Vector3.zero;
            return;
        }

        // ==========================================
        // [핵심 추가] 런치(강제 점프/튕김) 속도 덮어쓰기 로직
        // ==========================================
        if (_hasLaunchRequest)
        {
            if (_resetVerticalVelocity)
                currentVelocity = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp); // Y축 속도 완전 삭제(0)

            if (_resetHorizontalVelocity)
                currentVelocity = Vector3.Project(currentVelocity, Motor.CharacterUp); // XZ(수평) 속도 완전 삭제(0)

            currentVelocity += _launchVelocity; // 순수하게 점프/튕김 속도만 적용!
            _hasLaunchRequest = false;
        }

        // --- 기존 로직 ---
        float moveSpeedMultiplier = GetMoveSpeedMultiplier();
        float targetScalarSpeed = MoveSpeed * moveSpeedMultiplier;
        if (_moveInputVector.sqrMagnitude <= 0.0001f) targetScalarSpeed = 0f;

        targetScalarSpeed *= _inputMagnitude;

        if (_currentScalarSpeed < targetScalarSpeed - 0.1f || _currentScalarSpeed > targetScalarSpeed + 0.1f)
        {
            _currentScalarSpeed = Mathf.Lerp(_currentScalarSpeed, targetScalarSpeed, deltaTime * SpeedChangeRate);
            _currentScalarSpeed = Mathf.Round(_currentScalarSpeed * 1000f) / 1000f;
        }
        else _currentScalarSpeed = targetScalarSpeed;

        AnimationBlend = Mathf.Lerp(AnimationBlend, targetScalarSpeed, deltaTime * SpeedChangeRate);
        if (AnimationBlend < 0.01f) AnimationBlend = 0f;

        CurrentMoveSpeed = _currentScalarSpeed;
        Vector3 gravityVector = Motor.CharacterUp * Gravity;

        if (Motor.GroundingStatus.IsStableOnGround)
        {
            Vector3 groundNormal = Motor.GroundingStatus.GroundNormal;
            currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, groundNormal) * currentVelocity.magnitude;

            Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
            Vector3 reorientedInput = Vector3.Cross(groundNormal, inputRight).normalized * _moveInputVector.magnitude;
            Vector3 targetMovementVelocity = reorientedInput * _currentScalarSpeed;

            float stableSharpness = SpeedChangeRate;
            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-stableSharpness * deltaTime));
        }
        else
        {
            if (_moveInputVector.sqrMagnitude > 0f)
            {
                float maxAirMoveSpeed = _currentScalarSpeed;
                Vector3 addedVelocity = _moveInputVector * AirAccelerationSpeed * deltaTime;
                Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

                if (currentVelocityOnInputsPlane.magnitude < maxAirMoveSpeed)
                {
                    Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, maxAirMoveSpeed);
                    addedVelocity = newTotal - currentVelocityOnInputsPlane;
                }
                else if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
                {
                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                }

                if (Motor.GroundingStatus.FoundAnyGround && Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f)
                {
                    Vector3 perpendicularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpendicularObstructionNormal);
                }

                currentVelocity += addedVelocity;
            }

            if (!_windActive) currentVelocity += gravityVector * deltaTime;

            currentVelocity *= 1f / (1f + (AirDrag * deltaTime));
        }

        _jumpedThisFrame = false;
        _timeSinceJumpRequested += deltaTime;

        bool canJumpFromGround = Motor.GroundingStatus.IsStableOnGround || _timeSinceLastAbleToJump <= GroundedCoyoteTime;

        if (_jumpRequested && !_jumpConsumed && canJumpFromGround && _jumpTimeoutDelta <= 0f)
        {
            float jumpSpeed = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            Vector3 jumpDirection = Motor.CharacterUp;
            if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                jumpDirection = Motor.GroundingStatus.GroundNormal;

            Motor.ForceUnground();
            currentVelocity += (jumpDirection * jumpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
            _jumpRequested = false;
            _jumpConsumed = true;
            _jumpedThisFrame = true;
            JumpAnimActive = true;
        }

        if (_windActive)
        {
            currentVelocity -= Vector3.Project(currentVelocity, Motor.CharacterUp);
            currentVelocity += Motor.CharacterUp * _windLiftSpeed;
        }

        if (_externalImpact.sqrMagnitude > 0.04f)
        {
            currentVelocity += _externalImpact;
            bool isReallyGrounded = Grounded && Vector3.Dot(currentVelocity, Motor.CharacterUp) <= 0.01f;
            float friction = isReallyGrounded ? 5f : 1f;
            _externalImpact = Vector3.Lerp(_externalImpact, Vector3.zero, friction * deltaTime);
        }
        else _externalImpact = Vector3.zero;

        if (_conveyorVelocity.sqrMagnitude > 0f) currentVelocity += _conveyorVelocity;

        if (_internalVelocityAdd.sqrMagnitude > 0f)
        {
            currentVelocity += _internalVelocityAdd;
            _internalVelocityAdd = Vector3.zero;
        }
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        PhysicsGrounded = Motor.GroundingStatus.IsStableOnGround;
        isStableOnGround = Motor.GroundingStatus.IsStableOnGround;
        foundAnyGround = Motor.GroundingStatus.FoundAnyGround;

        if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime) _jumpRequested = false;

        if (PhysicsGrounded)
        {
            _ungroundedTimer = 0f;
            Grounded = true;
            _fallTimeoutDelta = FallTimeout;
            FreeFallAnimActive = false;

            if (!_jumpedThisFrame)
            {
                _jumpConsumed = false;
                JumpAnimActive = false;
            }

            _timeSinceLastAbleToJump = 0f;
            if (_jumpTimeoutDelta >= 0f) _jumpTimeoutDelta -= deltaTime;
        }
        else
        {
            _ungroundedTimer += deltaTime;
            Grounded = _ungroundedTimer < GroundedCoyoteTime;
            _jumpTimeoutDelta = JumpTimeout;
            _timeSinceLastAbleToJump += deltaTime;

            if (_fallTimeoutDelta >= 0f) _fallTimeoutDelta -= deltaTime;
            else FreeFallAnimActive = true;
        }

        _conveyorVelocity = Vector3.zero;
    }

    public void PostGroundingUpdate(float deltaTime) { }
    public bool IsColliderValidForCollisions(Collider coll) { return true; }
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }

    private float GetMoveSpeedMultiplier()
    {
        if (_runtime != null) return Mathf.Max(0f, _runtime.Stats.GetValue(EStatType.MoveSpeedMultiplier));
        if (!RunManager.TryGetInstance(out RunManager runManager)) return 1f;

        RunSession runSession = runManager.RunSession;
        if (runSession == null || runSession.Player == null) return 1f;

        return Mathf.Max(0f, runSession.Player.Stats.GetValue(EStatType.MoveSpeedMultiplier));
    }
}