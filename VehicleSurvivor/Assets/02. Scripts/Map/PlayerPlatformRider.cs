using KinematicCharacterController;
using UnityEngine;

[RequireComponent(typeof(KinematicCharacterMotor))]
public class PlayerPlatformRider : MonoBehaviour
{
    private KinematicCharacterMotor motor;
    private Transform currentPlatform;

    private Vector3 lastPlatformPosition;
    private Quaternion lastPlatformRotation;

    [Header("발판 감지 설정")]
    public LayerMask groundLayer;
    public string movingPlatformTag = "MovingPlatform";

    [Tooltip("빠르게 하강하는 발판을 놓치지 않도록 레이캐스트 길이를 살짝 늘려줍니다.")]
    public float groundCheckDistance = 0.5f;

    private void Awake()
    {
        motor = GetComponent<KinematicCharacterMotor>();
    }

    private void LateUpdate()
    {
        if (motor == null || motor.Capsule == null)
            return;

        CapsuleCollider capsule = motor.Capsule;
        Vector3 rayStart = transform.position + capsule.center;
        float castRadius = capsule.radius * 0.9f;
        float castDistance = (capsule.height / 2f) - castRadius + groundCheckDistance;

        if (Physics.SphereCast(rayStart, castRadius, Vector3.down, out RaycastHit hit, castDistance, groundLayer))
        {
            Transform hitPlatform = ResolvePlatformTransform(hit.collider.transform);
            if (hitPlatform != null)
            {
                if (currentPlatform != hitPlatform)
                {
                    currentPlatform = hitPlatform;
                    lastPlatformPosition = currentPlatform.position;
                    lastPlatformRotation = currentPlatform.rotation;
                }

                Vector3 deltaPosition = currentPlatform.position - lastPlatformPosition;
                Quaternion deltaRotation = currentPlatform.rotation * Quaternion.Inverse(lastPlatformRotation);

                Vector3 offsetFromCenter = transform.position - lastPlatformPosition;
                Vector3 rotatedOffset = deltaRotation * offsetFromCenter;
                Vector3 rotationMovement = rotatedOffset - offsetFromCenter;

                Vector3 finalMove = deltaPosition + rotationMovement;

                if (finalMove.sqrMagnitude > 0.00000001f)
                    motor.SetPosition(motor.TransientPosition + finalMove);

                lastPlatformPosition = currentPlatform.position;
                lastPlatformRotation = currentPlatform.rotation;
                return;
            }
        }

        currentPlatform = null;
    }

    private Transform ResolvePlatformTransform(Transform hitTransform)
    {
        Transform current = hitTransform;
        while (current != null)
        {
            if (current.CompareTag(movingPlatformTag))
                return current;

            current = current.parent;
        }

        return null;
    }
}
