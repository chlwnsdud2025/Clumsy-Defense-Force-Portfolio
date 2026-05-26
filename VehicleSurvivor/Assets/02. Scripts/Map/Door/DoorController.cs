using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    public Vector3 openOffset = new Vector3(0, -10f, 0); // 위로 5만큼 올라가며 열림 (또는 아래로)
    public float openSpeed = 2f;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isOpen = false;

    void Start()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + openOffset;
    }

    // 다른 기믹 스크립트들이 조건 달성 시 이 함수를 호출합니다!
    public void OpenDoor()
    {
        
        // 보스가 살아있다면 문이 열리지 않도록 강제 차단!
        if (StageRunManager.Instance != null && StageRunManager.Instance.isBossActive)
        {
            Debug.LogWarning("보스를 먼저 처치해야 문이 열립니다!");
            // 필요하다면 여기에 플레이어에게 보여줄 경고 UI 출력 로직을 추가할 수 있습니다.
            return;
        }

        if (!isOpen)
        {
            isOpen = true;
            StartCoroutine(OpenRoutine());
            Debug.Log("문이 열립니다!");
        }
    }

    private IEnumerator OpenRoutine()
    {
        while (Vector3.Distance(transform.position, openPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, openPosition, Time.deltaTime * openSpeed);
            yield return null;
        }
    }
}