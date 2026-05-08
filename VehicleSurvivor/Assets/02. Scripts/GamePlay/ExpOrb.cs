using UnityEngine;

public enum ExpType { Small, Medium, Large }

public class ExpOrb : MonoBehaviour
{
    public ExpType orbType;
    private ExpManager myManager;

    [HideInInspector]
    public int expValue; // 이제 스폰될 때 정확한 값을 주입받습니다.

    [Header("Bounce Settings")]
    public float bounceForce = 5f;
    public float scatterForce = 3f;
    public float gravity = 15f;

    private Vector3 currentVelocity;
    private bool isBouncing = false;
    [HideInInspector] public bool isMagnetized = false; // 자석 효과용

    public void Setup(ExpType type, ExpManager manager)
    {
        orbType = type;
        myManager = manager;
        // 하드코딩된 경험치량(1, 10, 50) 삭제!
    }

    // ★ Spawn 함수가 호출될 때 실제 경험치량(amount)을 받습니다.
    public void Spawn(Vector3 position, int amount)
    {
        transform.position = position;
        expValue = amount; // 몹이 넘겨준 정확한 경험치 저장
        gameObject.SetActive(true);

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        currentVelocity = new Vector3(randomDir.x * scatterForce, bounceForce, randomDir.y * scatterForce);
        isBouncing = true;
    }

    void Update()
    {
        if (isBouncing)
        {
            currentVelocity.y -= gravity * Time.deltaTime;
            transform.position += currentVelocity * Time.deltaTime;

            if (transform.position.y <= 0f)
            {
                Vector3 groundPos = transform.position;
                groundPos.y = 0f;
                transform.position = groundPos;
                isBouncing = false;
            }
        }
    }

    public void Collect()
    {
        // TODO: PlayerLevel.AddExp(expValue);
        myManager.ReturnToPool(this);
    }
}