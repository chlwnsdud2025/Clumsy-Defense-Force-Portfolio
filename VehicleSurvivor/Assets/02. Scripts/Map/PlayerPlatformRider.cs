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

    void Start()
    {
        _controller = GetComponent<CharacterController>();
    }

    void LateUpdate()
    {
        Vector3 rayStart = transform.position + _controller.center;
        float castRadius = _controller.radius * 0.9f;
        float castDistance = (_controller.height / 2f) - castRadius + 0.3f;

        if (Physics.SphereCast(rayStart, castRadius, Vector3.down, out RaycastHit hit, castDistance, groundLayer))
        {
            if (hit.transform.CompareTag(movingPlatformTag))
            {
                Transform hitPlatform = hit.transform;

                // 새로운 발판에 올라갔을 때 (기준점 리셋)
                if (_currentPlatform != hitPlatform)
                {
                    _currentPlatform = hitPlatform;
                    _lastPlatformPosition = _currentPlatform.position;
                    _lastPlatformRotation = _currentPlatform.rotation;
                }

                // 1. 발판의 위치 변화량(Delta) 계산
                Vector3 deltaPosition = _currentPlatform.position - _lastPlatformPosition;

                // 2. 발판의 회전 변화량(Delta) 계산
                Quaternion deltaRotation = _currentPlatform.rotation * Quaternion.Inverse(_lastPlatformRotation);

                // 3. [핵심 버그 수정!] 반드시 "이전 프레임의 발판 위치"를 기준으로 오프셋을 구해야 합니다.
                Vector3 offsetFromCenter = transform.position - _lastPlatformPosition;

                // 오프셋을 발판이 회전한 만큼 똑같이 돌려줍니다.
                Vector3 rotatedOffset = deltaRotation * offsetFromCenter;

                // 회전으로 인해 이동해야 할 거리
                Vector3 rotationMovement = rotatedOffset - offsetFromCenter;

                // 4. 최종 이동: 직선 이동량 + 회전 이동량
                Vector3 finalMove = deltaPosition + rotationMovement;

                // 미세한 덜덜거림 방지
                if (finalMove.magnitude > 0.0001f)
                {
                    _controller.Move(finalMove);
                }

                // 5. 플레이어의 시선(회전)도 발판의 회전량만큼 돌려줌
                transform.rotation = deltaRotation * transform.rotation;

                // 6. 다음 프레임을 위해 현재 발판 상태 저장
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