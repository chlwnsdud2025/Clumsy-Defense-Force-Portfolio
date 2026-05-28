using UnityEngine;

public class CheatNPC : MonoBehaviour
{
    private bool isPlayerInRange = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            Debug.Log("Cheat NPC 범위 진입: F8(스테이지 클리어), F7(보스 처치), F6(일반 몹 전멸) 사용 가능");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            Debug.Log("Cheat NPC 범위 이탈");
        }
    }

    private void Update()
    {
        // 플레이어가 범위 안에 있을 때만 단축키 입력을 받습니다.
        if (isPlayerInRange)
        {
            // F8 키: 스테이지 강제 클리어
            if (Input.GetKeyDown(KeyCode.F8))
            {
                GetKeyCheat();
            }

            // F7 키: 보스 처치
            if (Input.GetKeyDown(KeyCode.F7))
            {
                KillBoss();
            }

            // 🚀 F6 키: 일반 잡몹 전멸
            if (Input.GetKeyDown(KeyCode.F6))
            {
                KillAllEnemies();
            }
        }
    }

    public void GetKeyCheat()
    {
        Debug.Log("치트 발동: 스테이지 클리어 (F8)");
        if (StageRunManager.Instance != null)
        {
            StageRunManager.Instance.OnStageCleared();
        }
    }

    public void KillBoss()
    {
        Debug.Log("치트 발동: 필드 위 보스 즉사 (F7)");

        BossEnemy[] activeBosses = FindObjectsOfType<BossEnemy>();

        if (activeBosses.Length > 0)
        {
            foreach (BossEnemy boss in activeBosses)
            {
                boss.TakeDamage(DamageInfo.Normal(999999f));
            }
        }
        else
        {
            Debug.LogWarning("현재 필드에 처치할 보스가 없습니다.");
        }
    }

    // ==========================================
    // 🚀 [신규 추가] 필드 위의 일반 잡몹(Enemy) 전멸 로직
    // ==========================================
    public void KillAllEnemies()
    {
        Debug.Log("치트 발동: 필드 위 일반 잡몹 전멸 (F6)");

        // FindObjectsOfType은 하이어라키에서 현재 '활성화된(Active)' 오브젝트만 찾습니다.
        // 즉, 풀링(Pooling)되어 숨어있는 대기 몹들은 안전하게 무시됩니다!
        Enemy[] activeEnemies = FindObjectsOfType<Enemy>();

        if (activeEnemies.Length > 0)
        {
            foreach (Enemy enemy in activeEnemies)
            {
                enemy.TakeDamage(DamageInfo.Normal(999999f));
            }
            Debug.Log($"총 {activeEnemies.Length}마리의 잡몹을 처치했습니다.");
        }
        else
        {
            Debug.LogWarning("현재 필드에 처치할 잡몹이 없습니다.");
        }
    }
}