using UnityEngine;
using UnityEngine.InputSystem;

public class TestPlayer : MonoBehaviour
{
    [Header("Speed")]
    [Tooltip("전진 최대 속도")]
    [SerializeField] private float maxForwardSpeed = 20f;
    [Tooltip("후진 최대 속도")]
    [SerializeField] private float maxReverseSpeed = 8f;
    [Tooltip("속도가 증가하는 정도 (가속력)")]
    [SerializeField] private float acceleration = 15f;
    [Tooltip("속도가 줄어드는 정도 (감속력)")]
    [SerializeField] private float deceleration = 5f;
    [Tooltip("반대 방향 입력 시 먼저 속도를 죽이는 정도 (방향 전환 제동력)")]
    [SerializeField] private float directionChangeDeceleration = 20f;
    [Tooltip("부스터 사용 중 전진 최대 속도")]
    [SerializeField] private float boostMaxForwardSpeed = 30f;
    [Tooltip("부스터 사용 중 가속력")]
    [SerializeField] private float boostAcceleration = 25f;

    [Header("Turning")]
    [Tooltip("기본 회전 속도")]
    [SerializeField] private float turnSpeedDegrees = 140f;
    [Tooltip("회전이 가능한 최저 속도")]
    [SerializeField] private float minSpeedForTurn = 1f;
    [Tooltip("회전 중일 때 속도가 줄어드는 정도 (회전 시 감속력)")]
    [SerializeField] private float driftDeceleration = 6f;
    [Tooltip("드리프트 감속이 적용받는 최저 속도")]
    [SerializeField] private float minSpeedForDrift = 0.5f;

    [Header("VFX")]
    [Tooltip("회전 중일 때 재생할 파티클입니다.")]
    [SerializeField] private ParticleSystem turnSmokeParticle;

    private Vector2 moveInput;
    private float currentSpeed;

    private bool isBoosting;

    private void Update()
    {
        MoveArcadeCar(moveInput);
    }

    private void MoveArcadeCar(Vector2 input)
    {
        float forwardInput = input.y;
        float turnInput = input.x;

        UpdateSpeed(forwardInput, turnInput);
        UpdateRotation(forwardInput, turnInput);
        UpdatePosition();
        UpdateTurnSmokeParticle(turnInput);
    }

    private void UpdateSpeed(float forwardInput, float turnInput)
    {
        float currentMaxForwardSpeed = isBoosting ? boostMaxForwardSpeed : maxForwardSpeed;
        float currentAcceleration = isBoosting ? boostAcceleration : acceleration;

        if (forwardInput > 0f)
        {
            if (currentSpeed < 0f)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, directionChangeDeceleration * forwardInput * Time.deltaTime);
            }
            else
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, currentMaxForwardSpeed, currentAcceleration * forwardInput * Time.deltaTime);
            }
        }
        else if (forwardInput < 0f)
        {
            float reverseInput = -forwardInput;

            if (currentSpeed > 0f)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, directionChangeDeceleration * reverseInput * Time.deltaTime);
            }
            else
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, -maxReverseSpeed, currentAcceleration * reverseInput * Time.deltaTime);
            }
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
        }

        bool isTurning = Mathf.Abs(turnInput) > 0.1f;
        bool hasEnoughSpeedForDrift = Mathf.Abs(currentSpeed) > minSpeedForDrift;
        if (isTurning && hasEnoughSpeedForDrift)
        {
            float driftDecelerationAmount = driftDeceleration * Mathf.Abs(turnInput) * Time.deltaTime;
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, driftDecelerationAmount);
        }
    }

    private void UpdateRotation(float forwardInput, float turnInput)
    {
        if (Mathf.Abs(turnInput) < 0.01f)
            return;

        if (Mathf.Abs(currentSpeed) < minSpeedForTurn)
            return;

        float direction;
        if (Mathf.Abs(forwardInput) > 0.01f)
        {
            direction = forwardInput < 0f ? -1f : 1f;
        }
        else
        {
            direction = currentSpeed < 0f ? -1f : 1f;
        }

        transform.Rotate(0f, turnInput * turnSpeedDegrees * direction * Time.deltaTime, 0f);
    }

    private void UpdatePosition()
    {
        transform.position += transform.forward * currentSpeed * Time.deltaTime;
    }

    private void UpdateTurnSmokeParticle(float turnInput)
    {
        if (turnSmokeParticle == null)
            return;

        bool isTurning = Mathf.Abs(turnInput) > 0.01f;
        bool canTurn = Mathf.Abs(currentSpeed) >= minSpeedForTurn;
        bool shouldPlay = isTurning && canTurn;

        if (shouldPlay)
        {
            if (!turnSmokeParticle.isPlaying)
                turnSmokeParticle.Play();
        }
        else if (turnSmokeParticle.isPlaying)
        {
            turnSmokeParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnBooster(InputValue value)
    {
        isBoosting = value.isPressed;
    }
}
