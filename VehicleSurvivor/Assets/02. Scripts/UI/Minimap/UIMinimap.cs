using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIMinimap : MonoBehaviour
{
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private float zoomMin = 50f;
    [SerializeField] private float zoomMax = 250f;
    [SerializeField] private float zoomOneStep = 10f;
    [SerializeField] private TextMeshProUGUI textMapName;

    private void Awake()
    {
        //현재 활성화된 씬 이름을 가져와 UI에 표시
        textMapName.text = SceneManager.GetActiveScene().name;
    }
    public void ZoomIn()
    {
        minimapCamera.orthographicSize = Mathf.Max(minimapCamera.orthographicSize - zoomOneStep, zoomMin); // 둘중 큰값 반환
    }
    public void ZoomOut() 
    { 
        minimapCamera.orthographicSize = Mathf.Min(minimapCamera.orthographicSize + zoomOneStep, zoomMax); // 둘중 작은값 반환
    }


    }
