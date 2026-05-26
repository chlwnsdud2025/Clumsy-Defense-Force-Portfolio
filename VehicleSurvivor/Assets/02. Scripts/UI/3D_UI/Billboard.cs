using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    public Camera TargetCamera
    {
        get
        {
            ResolveCamera();
            return mainCamera;
        }
    }

    private void Start() => ResolveCamera();

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            ResolveCamera();
            return;
        }

        transform.forward = mainCamera.transform.forward;
    }

    private void ResolveCamera()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }
}
