using KinematicCharacterController;
using UnityEngine;

namespace ithappy.Battle_Stadium
{
    [RequireComponent(typeof(Rigidbody), typeof(PhysicsMover))]
    public class KccOscillatePlatform : MonoBehaviour, IMoverController
    {
        public PhysicsMover Mover;

        public Vector3 moveAxis = Vector3.up;
        public float moveDistance = 2f;
        public float duration = 2f;
        public bool useRandomDelay = false;
        public float maxRandomDelay = 1f;

        public bool useLocalAxes = true;

        private Vector3 startPosition;
        private Quaternion startRotation;

        private float timeElapsed = 0f;
        private bool isReversing = false;
        private float randomDelay = 0f;

        void Awake()
        {
            if (Mover == null)
                Mover = GetComponent<PhysicsMover>();

            // ==========================================
            // [핵심 수정] 첫 FixedUpdate가 돌기 전에 무조건 먼저 할당되도록
            // Start()가 아닌 Awake()에서 MoverController를 묶어줍니다!
            // ==========================================
            if (Mover != null)
                Mover.MoverController = this;

            startPosition = useLocalAxes ? transform.localPosition : transform.position;
            startRotation = useLocalAxes ? transform.localRotation : transform.rotation;

            if (useRandomDelay)
            {
                randomDelay = Random.Range(0f, maxRandomDelay);
            }
        }

        // Start() 함수는 아예 지워버려도 됩니다.

        public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
        {
            if (timeElapsed < randomDelay)
            {
                timeElapsed += deltaTime;
                CalculateGoalPose(startPosition, out goalPosition, out goalRotation);
                return;
            }

            float progress = (timeElapsed - randomDelay) / (duration / 2f);
            progress = Mathf.Clamp01(progress);
            progress = EaseInOut(progress);

            float currentDistance = moveDistance * (isReversing ? (1 - progress) : progress);
            Vector3 currentPosition = startPosition + moveAxis.normalized * currentDistance;

            CalculateGoalPose(currentPosition, out goalPosition, out goalRotation);

            timeElapsed += deltaTime;
            if (timeElapsed >= (duration / 2f) + randomDelay)
            {
                timeElapsed = randomDelay;
                isReversing = !isReversing;
            }
        }

        private void CalculateGoalPose(Vector3 targetPos, out Vector3 goalPosition, out Quaternion goalRotation)
        {
            if (useLocalAxes && transform.parent != null)
            {
                goalPosition = transform.parent.TransformPoint(targetPos);
                goalRotation = transform.parent.rotation * startRotation;
            }
            else
            {
                goalPosition = targetPos;
                goalRotation = startRotation;
            }
        }

        private float EaseInOut(float t)
        {
            return t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
        }
    }
}