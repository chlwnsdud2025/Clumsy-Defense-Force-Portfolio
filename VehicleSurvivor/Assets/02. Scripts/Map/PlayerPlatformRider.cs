using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerPlatformRider : MonoBehaviour
{
    private CharacterController _controller;
    private Transform _currentPlatform;

    private Vector3 _lastPlatformPosition;
    private Quaternion _lastPlatformRotation;

    [Header("발판 감지 설정")]
    public LayerMask groundLayer;
    public string movingPlatformTag = "MovingPlatform";

    [Tooltip("빠르게 하강하는 발판을 놓치지 않도록 레이캐스트 길이를 살짝 늘려줍니다.")]
    public float groundCheckDistance = 0.5f;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
    }

    void LateUpdate()
    {
        Vector3 rayStart = transform.position + _controller.center;
        float castRadius = _controller.radius * 0.9f;
        float castDistance = (_controller.height / 2f) - castRadius + groundCheckDistance;

        if (Physics.SphereCast(rayStart, castRadius, Vector3.down, out RaycastHit hit, castDistance, groundLayer))
        {
            if (hit.transform.CompareTag(movingPlatformTag))
            {
                Transform hitPlatform = hit.transform;

                if (_currentPlatform != hitPlatform)
                {
                    _currentPlatform = hitPlatform;
                    _lastPlatformPosition = _currentPlatform.position;
                    _lastPlatformRotation = _currentPlatform.rotation;
                }

                // 1. 변화량 계산
                Vector3 deltaPosition = _currentPlatform.position - _lastPlatformPosition;
                Quaternion deltaRotation = _currentPlatform.rotation * Quaternion.Inverse(_lastPlatformRotation);

                Vector3 offsetFromCenter = transform.position - _lastPlatformPosition;
                Vector3 rotatedOffset = deltaRotation * offsetFromCenter;
                Vector3 rotationMovement = rotatedOffset - offsetFromCenter;

                Vector3 finalMove = deltaPosition + rotationMovement;

                // 2. [핵심 버그 수정!] Move()를 쓰지 않고 컨트롤러를 잠시 끈 뒤 강제로 위치/회전을 맞춥니다.
                if (finalMove.magnitude > 0.0001f || deltaRotation != Quaternion.identity)
                {
                    _controller.enabled = false; // 충돌 무시 (가장 중요)

                    transform.position += finalMove;
                    transform.rotation = deltaRotation * transform.rotation;

                    _controller.enabled = true;  // 이동 후 복구
                }

                _lastPlatformPosition = _currentPlatform.position;
                _lastPlatformRotation = _currentPlatform.rotation;
            }
            else
            {
                _currentPlatform = null;
            }
        }
        else
        {
            _currentPlatform = null;
        }
    }
}