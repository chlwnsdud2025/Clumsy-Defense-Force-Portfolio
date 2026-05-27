using System.Collections.Generic;
using UnityEngine;

public enum ExpType { Small, Medium, Large }

public class ExpOrb : MonoBehaviour
{
    public ExpType orbType;
    private ExpManager myManager;

    [HideInInspector]
    public int expValue;

    // [추가됨] 매니저의 활성 리스트에서 자신의 위치를 기억하는 변수 (O(1) 삭제용)
    [HideInInspector]
    public LinkedListNode<ExpOrb> activeNode;

    [Header("Bounce Settings")]
    public float bounceForce = 5f;
    public float scatterForce = 3f;
    public float gravity = 15f;

    [Header("Ground Placement")]
    public float groundOffset = 0f;

    // ==========================================
    // ⏳ [핵심 추가] 수명(방치) 시스템
    // ==========================================
    [Header("LifeTime Settings")]
    [Tooltip("필드에 방치된 채 이 시간이 지나면 자동으로 풀로 돌아갑니다.")]
    public float maxLifeTime = 120f;
    private float currentLifeTime = 0f;

    private Vector3 currentVelocity;
    private bool isBouncing = false;
    private float targetGroundY;
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

        targetGroundY = position.y + groundOffset;

        gameObject.SetActive(true);

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        currentVelocity = new Vector3(randomDir.x * scatterForce, bounceForce, randomDir.y * scatterForce);

        isBouncing = true;
        consumed = false;
        isMagnetized = false;

        // [추가됨] 스폰될 때 수명 타이머 초기화
        currentLifeTime = 0f;
    }

    void Update()
    {
        // 바운스 물리 효과
        if (isBouncing)
        {
            currentVelocity.y -= gravity * Time.deltaTime;
            transform.position += currentVelocity * Time.deltaTime;

            if (transform.position.y <= targetGroundY)
            {
                Vector3 groundPos = transform.position;
                groundPos.y = targetGroundY;
                transform.position = groundPos;
                isBouncing = false;
            }
        }

        // ==========================================
        // ⏳ [핵심 추가] 너무 오래 방치된 구슬 회수 로직
        // ==========================================
        if (!isMagnetized && !consumed) // 자석으로 끌려가는 중이 아닐 때만 수명 감소
        {
            currentLifeTime += Time.deltaTime;
            if (currentLifeTime >= maxLifeTime)
            {
                // 수명이 다 되면 플레이어가 안 먹은 것으로 간주하고 풀로 복귀!
                Collect();
            }
        }
    }

    public void Collect()
    {
        if (consumed) return;
        consumed = true;

        // [수정됨] 매니저에 정의된 ReturnToPool 호출
        myManager.ReturnToPool(this);
    }

    public void ApplyPullTowards(Vector3 worldTarget, float maxStepDistance)
    {
        if (maxStepDistance <= 0f)
            return;

        if (isBouncing)
            isBouncing = false;

        // 끌려가기 시작하면 자석 상태 On (이때부터는 수명 타이머가 멈춤)
        isMagnetized = true;

        transform.position = Vector3.MoveTowards(transform.position, worldTarget, maxStepDistance);
    }

    // (기존의 TryConsume 함수 등은 동일하게 유지)
    public bool TryConsume(out int amount)
    {
        amount = expValue;
        if (consumed) return false;

        Collect();
        return true;
    }
}