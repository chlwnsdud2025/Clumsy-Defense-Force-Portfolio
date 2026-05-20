using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        // 최적화를 위해 메인 카메라를 캐싱해둡니다.
        mainCamera = Camera.main;
    }

    // Update가 아닌 LateUpdate를 사용하는 이유:
    // 카메라가 이동을 마친 '후'에 텍스트가 카메라를 바라보게 해야 떨림(Jittering)이 없습니다.
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // 텍스트의 앞면이 카메라의 앞면과 같은 방향을 보게 합니다.
            transform.forward = mainCamera.transform.forward;
        }
    }
}