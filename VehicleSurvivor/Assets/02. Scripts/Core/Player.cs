using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Car Move")]
    public float normalSpeed = 6f;
    public float boostSpeed = 15f;
    public float acceleration = 3f;
    public float deceleration = 4f;
    public float turnSpeed = 120f;

    [Header("Wheels")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;

    [Header("Wheel Settings")]
    public float wheelRotateSpeed = 500f;
    public float maxSteerAngle = 30f;

    private Vector2 moveInput;
    private float currentSpeed;

    private void Update()
    {
        float move = moveInput.y;
        float turn = moveInput.x;

        bool isBoosting = Keyboard.current.leftCtrlKey.isPressed;

        float targetSpeed;

        if (Mathf.Abs(move) < 0.01f)
        {
            targetSpeed = 0f;
        }
        else if (isBoosting)
        {
            targetSpeed = boostSpeed;
        }
        else
        {
            targetSpeed = normalSpeed;
        }

        float speedChangeRate = currentSpeed < targetSpeed ? acceleration : deceleration;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            speedChangeRate * Time.deltaTime
        );

        transform.position += transform.forward * move * currentSpeed * Time.deltaTime;
        transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f);

        RotateWheels(move);
        SteerFrontWheels(turn);
    }

    private void RotateWheels(float move)
    {
        float rotateAmount = move * wheelRotateSpeed * currentSpeed * 0.1f * Time.deltaTime;

        if (frontLeftWheel != null) frontLeftWheel.Rotate(rotateAmount, 0f, 0f);
        if (frontRightWheel != null) frontRightWheel.Rotate(rotateAmount, 0f, 0f);
        if (rearLeftWheel != null) rearLeftWheel.Rotate(rotateAmount, 0f, 0f);
        if (rearRightWheel != null) rearRightWheel.Rotate(rotateAmount, 0f, 0f);
    }

    private void SteerFrontWheels(float turn)
    {
        float steerAngle = turn * maxSteerAngle;

        if (frontLeftWheel != null)
        {
            Vector3 rot = frontLeftWheel.localEulerAngles;
            rot.y = steerAngle;
            frontLeftWheel.localEulerAngles = rot;
        }

        if (frontRightWheel != null)
        {
            Vector3 rot = frontRightWheel.localEulerAngles;
            rot.y = steerAngle;
            frontRightWheel.localEulerAngles = rot;
        }
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
}