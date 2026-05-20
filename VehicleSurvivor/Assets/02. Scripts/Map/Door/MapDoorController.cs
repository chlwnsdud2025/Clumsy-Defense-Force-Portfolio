using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MapDoorController : MonoBehaviour
{
    [SerializeField] private Vector3 openOffset = new Vector3(0f, -10f, 0f);
    [SerializeField] private float openSpeed = 2f;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isOpen;
    private Coroutine openRoutine;

    private void Awake()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + openOffset;
    }

    public void OpenDoor()
    {
        if (isOpen)
            return;

        isOpen = true;
        if (openRoutine != null)
            StopCoroutine(openRoutine);

        openRoutine = StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        while (Vector3.Distance(transform.position, openPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, openPosition, Time.deltaTime * openSpeed);
            yield return null;
        }

        transform.position = openPosition;
        openRoutine = null;
    }
}
