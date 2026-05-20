using UnityEngine;

public class StageClearObject : MonoBehaviour
{
    // 플레이어가 기믹을 풀거나 트리거에 닿았을 때 호출
    public void Interact()
    {
        if (StageRunManager.Instance != null)
        {
            StageRunManager.Instance.OnStageCleared();
        }
        Destroy(gameObject); // 작동 후 비활성화 또는 파괴
    }
}