using UnityEngine;

public class TwinBossManager : MonoBehaviour
{
    [Header("Twin Boss Settings")]
    public int totalBossCount = 2;
    private int deadCount = 0;

    public void OnTwinDied()
    {
        deadCount++;
        Debug.Log($"쌍둥이 보스 처치 현황: {deadCount} / {totalBossCount}");

        if (deadCount >= totalBossCount)
        {
            Debug.Log("쌍둥이 보스 완전 토벌 완료! 스테이지 클리어!");

            if (StageRunManager.Instance != null)
            {
                StageRunManager.Instance.OnMidBossDefeated();
            }

            Destroy(gameObject);
        }
    }
}