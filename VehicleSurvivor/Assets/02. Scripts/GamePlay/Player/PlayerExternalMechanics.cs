using UnityEngine;
using System.Reflection;

[RequireComponent(typeof(CharacterController))]
public class PlayerExternalMechanics : MonoBehaviour
{
    private CharacterController _controller;
    private PlayerController _playerController;
    private FieldInfo _verticalVelocityField;

    private Vector3 _externalImpact = Vector3.zero;

    // 윈드존 변수가 이렇게 간단해집니다!
    private bool _isWindActive = false;
    private float _windLiftSpeed = 0f;

    [Header("Impact Friction Settings")]
    public float airFriction = 1.0f;
    public float groundFriction = 5.0f;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _playerController = GetComponent<PlayerController>();

        if (_playerController != null)
        {
            _verticalVelocityField = typeof(PlayerController).GetField("_verticalVelocity", BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }

    void Update()
    {
        // 1. 점프대 연산
        if (_externalImpact.magnitude > 0.2f)
        {
            _controller.Move(_externalImpact * Time.deltaTime);

            float currentVerticalVelocity = _verticalVelocityField != null ? (float)_verticalVelocityField.GetValue(_playerController) : 0f;
            bool isReallyGrounded = _playerController != null && _playerController.Grounded && currentVerticalVelocity <= 0f;
            float friction = isReallyGrounded ? groundFriction : airFriction;

            _externalImpact = Vector3.Lerp(_externalImpact, Vector3.zero, friction * Time.deltaTime);
        }
        else
        {
            _externalImpact = Vector3.zero;
        }

        // ==========================================
        // 2. 윈드존 연산 (상승 기능만 유지)
        // ==========================================
        if (_isWindActive && _verticalVelocityField != null && _playerController != null)
        {
            float gravity = _playerController.Gravity;
            float frameGravityEffect = gravity * Time.deltaTime;

            // 원본 스크립트의 중력을 상쇄하며 우리가 정한 속도(_windLiftSpeed)로 일정하게 밀어 올립니다.
            _verticalVelocityField.SetValue(_playerController, _windLiftSpeed - frameGravityEffect);
        }
    }

    public void ApplyTargetHeightJump(float targetHeight, float horizontalForce, Vector3 direction)
    {
        _externalImpact = Vector3.zero;

        if (_playerController != null && _verticalVelocityField != null)
        {
            float gravity = _playerController.Gravity;
            float requiredVelocity = Mathf.Sqrt(targetHeight * -2f * gravity);
            _verticalVelocityField.SetValue(_playerController, requiredVelocity);
        }

        _externalImpact.x = direction.x * horizontalForce;
        _externalImpact.z = direction.z * horizontalForce;
        _externalImpact.y = 0f;
    }

    // [수정됨] 정점 플래그(isAtApex)가 아예 필요 없어졌습니다!
    public void SetWindForce(float liftSpeed)
    {
        _windLiftSpeed = liftSpeed;
        _isWindActive = true;
    }

    public void RemoveWindForce()
    {
        _windLiftSpeed = 0f;
        _isWindActive = false;
    }
}