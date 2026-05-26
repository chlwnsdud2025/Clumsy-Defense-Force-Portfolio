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
        if (other.CompareTag("Player"))
        {
            // ==========================================
            // 🔒 [핵심 방어] 보스가 살아있으면 버튼이 아예 안 눌림!
            // ==========================================
            if (StageRunManager.Instance != null && StageRunManager.Instance.isBossActive)
            {
                Debug.LogWarning("보스를 먼저 처치해야 버튼을 누를 수 있습니다!");
                return; // 상태 변경 없이 즉시 종료
            }

            // 정상적으로 버튼이 눌리는 로직
            if (!isPressed)
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
}