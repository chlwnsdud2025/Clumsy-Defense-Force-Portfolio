using UnityEngine;

public class BossStatue : MonoBehaviour, IDamageable
{
    [Header("Statue Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Tooltip("불이 들어왔을 때 켜질 이펙트나 조명")]
    public GameObject activeVfx;

    [Tooltip("부서졌을 때 터질 폭발 이펙트")]
    public GameObject destroyVfx;

    private bool isActive = false;
    private FinalBoss myBoss;

    // 충돌 데미지 -> 0
    public  float contactDamage = 0f;
    public float ContactDamage => contactDamage;
    public void Setup(FinalBoss boss)
    {
        myBoss = boss;
        currentHealth = maxHealth;

        // 처음엔 불이 꺼져있고 공격받지 않음
        if (activeVfx != null) activeVfx.SetActive(false);
        isActive = false;
    }

    // 보스가 체력이 깎여서 이 석상을 호출할 때 실행됨
    public void ActivateStatue()
    {
        isActive = true;
        if (activeVfx != null) activeVfx.SetActive(true);
        Debug.Log($"{gameObject.name} 석상에 불이 들어왔습니다! 파괴해야 합니다!");
    }

    // IDamageable 인터페이스 구현 (플레이어가 때릴 때)
    public void TakeDamage(float damage)
    {
        if (!isActive) return; // 불이 안 들어왔을 땐 데미지 무시

        currentHealth -= damage;
        Debug.Log("석상 체력 : " + currentHealth);
        // 데미지 텍스트 띄우기 (기존 보스 시스템 활용)
        if (DamageTextPool.Instance != null)
        {
            GameObject textObj = DamageTextPool.Instance.GetDamageText();
            textObj.GetComponent<DamageText>().Setup(damage, transform.position + Vector3.up * 2f);
        }

        if (currentHealth <= 0)
        {
            BreakStatue();
        }
    }

    private void BreakStatue()
    {
        isActive = false;

        if (destroyVfx != null)
        {
            Instantiate(destroyVfx, transform.position, Quaternion.identity);
        }

        Debug.Log("석상 부숴짐");


        // 보스에게 "나 부서졌어! 무적 풀어!" 라고 알림
        if (myBoss != null)
        {
            myBoss.OnStatueDestroyed();
        }

        Destroy(gameObject);
    }
}