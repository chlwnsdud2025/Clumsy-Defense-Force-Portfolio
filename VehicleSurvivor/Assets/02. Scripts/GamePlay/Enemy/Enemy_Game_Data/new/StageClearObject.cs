using UnityEngine;

public class StageClearObject : MonoBehaviour
{
    public void Interact()
    {
        if (StageRunManager.Instance != null)
        {
            StageRunManager.Instance.OnStageCleared();
        }
        Destroy(gameObject); // 작동 후 비활성화 또는 파괴
    }


    private void OnTriggerEnter(Collider other)
    {
        Interact();

    }
}