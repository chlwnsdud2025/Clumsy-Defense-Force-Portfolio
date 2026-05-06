using UnityEngine;

public class CarWheelMove : MonoBehaviour
{
    [Header("Car Move")]
    //자동차 이동속도
    public float moveSpeed = 6f;
    //자동차 회전 속도
    public float turnSpeed = 120f;

    [Header("Wheels")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;

    //바퀴가 얼마나 빨리 굴러갈지
    public float wheelRotateSpeed = 500;

    void Update()
    {
        float moveInput = Input.GetAxisRaw("Vertical"); //W.S 입력받기
        float turnInput = Input.GetAxisRaw("Horizontal");   // A, D 입력받기

        //앞뒤로 이동
        transform.position += transform.forward * moveInput * moveSpeed * Time.deltaTime;

        //좌우 회전
        transform.Rotate(0f, turnInput * turnSpeed * Time.deltaTime, 0f);

        //바퀴 굴러가는 회전
        RotateWheels(moveInput);
    }
    void RotateWheels(float moveInput)
    {
        float rotateAmount = moveInput * wheelRotateSpeed * Time.deltaTime;

        frontLeftWheel.Rotate(rotateAmount, 0f, 0f);
        frontRightWheel.Rotate(rotateAmount, 0f, 0f);
        rearLeftWheel.Rotate(rotateAmount, 0f, 0f);
        rearRightWheel.Rotate(rotateAmount, 0f, 0f);
    }
}
