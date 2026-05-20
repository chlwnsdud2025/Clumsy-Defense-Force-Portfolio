using UnityEngine;

public class ButtonTrigger : MonoBehaviour
{
    [Header("Connections")]
    public DoorController targetDoor; // 인스펙터에서 열릴 문 연결

    [Header("Settings")]
    public float pressDepth = 0.5f; // 버튼이 눌리는 깊이
    private bool isPressed = false;
    private Vector3 originalPos;

    void Start()
    {
        originalPos = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isPressed && other.CompareTag("Player"))
        {
            isPressed = true;

            // 시각적으로 버튼이 눌리는 연출
            transform.position = originalPos - new Vector3(0, pressDepth, 0);
            GetComponent<Renderer>().material.color = Color.green;

            // 문 열기 호출!
            if (targetDoor != null) targetDoor.OpenDoor();
        }
    }
}