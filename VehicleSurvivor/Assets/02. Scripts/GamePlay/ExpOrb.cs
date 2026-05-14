using Unity.Entities;
using UnityEngine;

public enum ExpType { Small, Medium, Large }

public class ExpOrb : MonoBehaviour
{
    public ExpType orbType;
    private ExpManager myManager;

    [HideInInspector]
    public int expValue;

    [Header("Bounce Settings")]
    public float bounceForce = 5f;
    public float scatterForce = 3f;
    public float gravity = 15f;

    [Header("Ground Placement")]
    public float groundOffset = 0f; // 프리팹 모델의 피벗에 맞춰 착지 높이를 미세 조정할 수 있는 변수

    private Vector3 currentVelocity;
    private bool isBouncing = false;
    private float targetGroundY; // 구슬이 착지해야 할 목표 Y 좌표

    private bool consumed;

    [HideInInspector] public bool isMagnetized = false;

    public void Setup(ExpType type, ExpManager manager)
    {
        orbType = type;
        myManager = manager;
    }

    public void Spawn(Vector3 position, int amount)
    {
        transform.position = position;
        expValue = amount;

        // 몬스터가 죽은 위치의 Y좌표를 바닥 기준으로 삼고, 오프셋을 더해줌
        targetGroundY = position.y + groundOffset;

        gameObject.SetActive(true);

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        currentVelocity = new Vector3(randomDir.x * scatterForce, bounceForce, randomDir.y * scatterForce);
        isBouncing = true;
        consumed = false;
    }

    void Update()
    {
        if (isBouncing)
        {
            currentVelocity.y -= gravity * Time.deltaTime;
            transform.position += currentVelocity * Time.deltaTime;

            // 무조건 0f가 아니라, 스폰 시 계산해둔 targetGroundY 이하로 내려가면 멈추도록 변경
            if (transform.position.y <= targetGroundY)
            {
                Vector3 groundPos = transform.position;
                groundPos.y = targetGroundY;
                transform.position = groundPos;
                isBouncing = false;
            }
        }
    }

    public void Collect()
    {
        myManager.ReturnToPool(this);
    }

    public void ApplyPullTowards(Vector3 worldTarget, float maxStepDistance)
    {
        if (maxStepDistance <= 0f)
            return;

        if (isBouncing)
            isBouncing = false;

        transform.position = Vector3.MoveTowards(transform.position, worldTarget, maxStepDistance);
    }

    public bool TryConsume(out int amount)
    {
        if (consumed)
        {
            amount = 0;
            return false;
        }

        consumed = true;
        amount = expValue;

        Collect();
        return true;
    }
}