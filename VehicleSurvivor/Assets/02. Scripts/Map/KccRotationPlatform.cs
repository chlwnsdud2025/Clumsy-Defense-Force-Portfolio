using KinematicCharacterController;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(PhysicsMover))]
public class KccRotationPlatform : MonoBehaviour, IMoverController
{
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    public PhysicsMover Mover;
    public RotationAxis rotationAxis = RotationAxis.Y;
    public float rotationSpeed = 50f;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 initialWorldPosition;
    private Quaternion initialWorldRotation;
    private float accumulatedAngle;

    private void Awake()
    {
        CacheBasePose();

        if (Mover == null)
            Mover = GetComponent<PhysicsMover>();

        // ==========================================
        // [핵심 수정] Start()에 있던 코드를 Awake()로 가져왔습니다!
        // 첫 FixedUpdate가 돌기 전에 안전하게 조종자(MoverController) 할당 완료
        // ==========================================
        if (Mover != null)
            Mover.MoverController = this;
    }

    private void OnEnable()
    {
        CacheBasePose();
        accumulatedAngle = 0f;
    }

    // Start() 함수는 이제 필요 없으므로 깔끔하게 삭제했습니다.

    public void Configure(RotationAxis axis, float speed)
    {
        rotationAxis = axis;
        rotationSpeed = speed;
        CacheBasePose();
        accumulatedAngle = 0f;
    }

    public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
    {
        accumulatedAngle += rotationSpeed * deltaTime;
        Quaternion spin = Quaternion.AngleAxis(accumulatedAngle, GetLocalAxis());
        Quaternion localRotation = spin * baseLocalRotation;

        Transform parent = transform.parent;
        if (parent != null)
        {
            goalRotation = parent.rotation * localRotation;
            goalPosition = parent.TransformPoint(baseLocalPosition);
            return;
        }

        goalPosition = initialWorldPosition;
        goalRotation = Quaternion.AngleAxis(accumulatedAngle, GetWorldAxis()) * initialWorldRotation;
    }

    private void CacheBasePose()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
        initialWorldPosition = transform.position;
        initialWorldRotation = transform.rotation;
    }

    private Vector3 GetLocalAxis()
    {
        return rotationAxis switch
        {
            RotationAxis.X => Vector3.right,
            RotationAxis.Y => Vector3.up,
            RotationAxis.Z => Vector3.forward,
            _ => Vector3.up
        };
    }

    private Vector3 GetWorldAxis()
    {
        return rotationAxis switch
        {
            RotationAxis.X => initialWorldRotation * Vector3.right,
            RotationAxis.Y => initialWorldRotation * Vector3.up,
            RotationAxis.Z => initialWorldRotation * Vector3.forward,
            _ => Vector3.up
        };
    }
}