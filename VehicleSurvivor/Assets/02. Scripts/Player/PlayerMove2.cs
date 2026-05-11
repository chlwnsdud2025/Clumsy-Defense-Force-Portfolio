using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove2 : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 8f;
    public float acceleration = 8f;
    public float deceleration = 6f;

    [Header("Rotation")]
    public float turnSpeed = 10f;

    [Header("Drift / Slide")]
    public float driftSmoothness = 5f;

    private Vector2 moveInput;
    private Vector3 currentVelocity;

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void Update()
    {
        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        if (inputDir != Vector3.zero)
        {
            Vector3 targetVelocity = inputDir * moveSpeed;

            currentVelocity = Vector3.Lerp(
                currentVelocity,
                targetVelocity,
                acceleration * Time.deltaTime
            );

            Quaternion targetRotation = Quaternion.LookRotation(inputDir);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
        }
        else
        {
            currentVelocity = Vector3.Lerp(
                currentVelocity,
                Vector3.zero,
                deceleration * Time.deltaTime
            );
        }

        transform.position += currentVelocity * Time.deltaTime;
    }
}