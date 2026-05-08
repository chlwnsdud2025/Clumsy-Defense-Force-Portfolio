using UnityEngine;

public enum ExpType { Small, Medium, Large }

public class ExpOrb : MonoBehaviour
{
    public ExpType orbType;
    private ExpManager myManager;

    [HideInInspector]
    public int expValue;

    [Header("Bounce Settings")]
    public float bounceForce = 5f;   // 위로 튀어 오르는 힘
    public float scatterForce = 3f;  // 옆으로 퍼지는 힘
    public float gravity = 15f;      // 떨어지는 중력 

    private Vector3 currentVelocity; // 현재 이동 속도/방향
    private bool isBouncing = false; // 튀어 오르는 중인지 체크

    public void Setup(ExpType type, ExpManager manager)
    {
        orbType = type;
        myManager = manager;

        if (type == ExpType.Small) expValue = 1;
        else if (type == ExpType.Medium) expValue = 10;
        else if (type == ExpType.Large) expValue = 50;
    }

    // 풀에서 꺼내질 때 호출 (여기서 초기 점프 힘을 줍니다!)
    public void Spawn(Vector3 position)
    {
        transform.position = position;
        gameObject.SetActive(true);

        // 1. XZ 평면으로 무작위로 뻗어나갈 방향 계산 (테두리 방향)
        Vector2 randomDir = Random.insideUnitCircle.normalized;

        // 2. 초기 속도 세팅: 옆으로 퍼지는 힘(X, Z) + 위로 솟구치는 힘(Y)
        currentVelocity = new Vector3(randomDir.x * scatterForce, bounceForce, randomDir.y * scatterForce);

        isBouncing = true; // 통통 튀기 시작!
    }

    // 구슬 스스로 이동하는 로직 (땅에 닿으면 연산을 멈춰서 최적화)
    void Update()
    {
        if (isBouncing)
        {
            // 1. 매 프레임마다 아래로 끌어내리는 가짜 중력 적용
            currentVelocity.y -= gravity * Time.deltaTime;

            // 2. 현재 속도만큼 이동
            transform.position += currentVelocity * Time.deltaTime;

            // 3. 땅(Y=0)에 닿았는지 체크
            // (만약 맵의 바닥 높이가 0이 아니라면 이 값을 수정하세요)
            if (transform.position.y <= 0f)
            {
                // 땅에 딱 붙이고, 더 이상 Update 연산을 하지 않도록 끕니다.
                Vector3 groundPos = transform.position;
                groundPos.y = 0f;
                transform.position = groundPos;

                isBouncing = false;
            }
        }
    }

    public void Collect()
    {
        myManager.ReturnToPool(this);
    }
}