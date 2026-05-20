using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    public Vector3 openOffset = new Vector3(0, -10f, 0); // 위로 5만큼 올라가며 열림
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