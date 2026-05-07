using UnityEngine;
using UnityEngine.InputSystem;
public class Player : MonoBehaviour
{
    public float movespeed = 5;
    public GameObject shootPrefeb;
    public Transform shootPoint;

    public float fireRate = 0.5f;
    private float fireTimer;

    private Vector2 moveInput;

    private void Update()
    {
        //vector2 입력값을 3D 방향으로 변환
        Vector3 moveDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        //이동
        transform.position += moveDir * movespeed * Time.deltaTime;
        
        //이동 방향 바라보기
        if(moveDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDir);  
        }

        //자동 발사 타이머
        fireTimer += Time.deltaTime;
        if(fireTimer >= fireRate)
        {
            Instantiate(shootPrefeb, shootPoint.position, transform.rotation);

            fireTimer = 0f;
        }
    }

    // Input System Move 이벤트 함수
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
}
