using UnityEngine;
using System.Reflection; // [핵심] 원본 수정 없이 private 변수에 접근하기 위해 필요

[RequireComponent(typeof(CharacterController))]
public class PlayerExternalMechanics : MonoBehaviour
{
    private CharacterController _controller;
    private PlayerController _playerController;
    private FieldInfo _verticalVelocityField; // 원본 스크립트의 숨겨진 중력 변수

    private Vector3 _externalImpact = Vector3.zero;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _playerController = GetComponent<PlayerController>();

        // C# Reflection: 원본 코드를 건드리지 않고 private _verticalVelocity 변수에 연결
        if (_playerController != null)
        {
            _verticalVelocityField = typeof(PlayerController).GetField("_verticalVelocity", BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }

    void Update()
    {
        // 1. 점프대 같은 순간적인 관성 힘 처리 (서서히 줄어듦)
        if (_externalImpact.magnitude > 0.2f)
        {
            _controller.Move(_externalImpact * Time.deltaTime);
            _externalImpact = Vector3.Lerp(_externalImpact, Vector3.zero, 5f * Time.deltaTime);
        }
        else
        {
            _externalImpact = Vector3.zero;
        }
    }

    public void ApplyTargetHeightJump(float targetHeight, float horizontalForce, Vector3 direction)
    {
        if (_playerController != null && _verticalVelocityField != null)
        {
            // 원본 스크립트에서 현재 맵의 중력 값을 가져옵니다.
            float gravity = _playerController.Gravity;

            // 물리학 공식: 목표 높이에 도달하기 위해 필요한 정확한 수직(Y축) 초기 속도 계산
            float requiredVelocity = Mathf.Sqrt(targetHeight * -2f * gravity);

            // 떨어지면서 누적된 마이너스 중력을 싹 무시하고, 계산된 점프 속도로 완벽하게 덮어씌움!
            _verticalVelocityField.SetValue(_playerController, requiredVelocity);
        }

        // X, Z 방향으로 날려보내는 힘 처리 (대각선 점프대용)
        // 수직(Vector3.up) 점프대라면 direction.x, z가 0이므로 제자리 점프가 됩니다.
        _externalImpact.x = direction.x * horizontalForce;
        _externalImpact.z = direction.z * horizontalForce;
        _externalImpact.y = 0f; // Y축은 위에서 원본 변수를 직접 조작했으므로 0으로 둡니다.
    }

    // 트랙터 빔(바람)에 의해 특정 방향으로 강제 견인되는 로직
    public void ApplyTractorBeam(Vector3 moveDelta, bool suspendGravity)
    {
        if (_verticalVelocityField != null && _playerController != null)
        {
            // [핵심 변경점] 정점에 도달했을 때는 중력을 0으로 만들어 덜덜거리는 추락을 원천 차단합니다.
            _verticalVelocityField.SetValue(_playerController, suspendGravity ? 0f : -2f);
        }

        if (moveDelta != Vector3.zero)
        {
            _controller.Move(moveDelta);
        }
    }
}