using UnityEngine;

public class CopyPosition : MonoBehaviour
{
    [SerializeField] private Transform target; // 쫓아가야할 대상
    [SerializeField] private bool x, y, z; // 이 값이 true 이면 타겟의 좌표, false 이면 현재 좌표를 그대로 사용
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null) return;
        transform.position = new Vector3(
            x ? target.position.x : transform.position.x,
            y ? target.position.y : transform.position.y,
            z ? target.position.z : transform.position.z);
    }
}
