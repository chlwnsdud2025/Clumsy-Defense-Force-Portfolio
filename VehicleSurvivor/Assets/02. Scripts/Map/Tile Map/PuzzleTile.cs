using UnityEngine;
using System.Collections;

public class PuzzleTile : MonoBehaviour
{
    public bool isTruePath = false; // 진짜 길인지 여부
    private bool isStepped = false; // 이미 밟은 타일인지 확인

    private Renderer tileRenderer;
    private Color originalColor;

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();
        originalColor = tileRenderer.material.color;
    }

    // 플레이어가 타일을 밟았을 때 (트리거나 콜라이더 충돌 기준)
    private void OnCollisionEnter(Collision collision)
    {
        // [추가된 디버그] 무엇이 부딪혔는지 이름과 태그를 먼저 확인합니다.
        Debug.Log($"충돌 발생! 부딪힌 오브젝트: {collision.gameObject.name}, 태그: {collision.gameObject.tag}, 현재 isStepped: {isStepped}");

        // 플레이어 태그가 아닐 경우 무시
        if (!collision.gameObject.CompareTag("Player") || isStepped)
        {
            return; // 여기서 코드가 종료됨!
        }

        isStepped = true;

        if (isTruePath)
        {
            Debug.Log("진짜 타일 밟음");
            tileRenderer.material.color = Color.green;
        }
        else
        {
            Debug.Log("가짜 타일 밟음");
            tileRenderer.material.color = Color.red;
            StartCoroutine(FallDown());
        }
    }

    private IEnumerator FallDown()
    {
        // 0.2초 정도 시각적 딜레이를 주어 플레이어가 당황할 시간을 줍니다.
        yield return new WaitForSeconds(0.2f);

        // Rigidbody를 활성화하여 아래로 떨어뜨리거나, 콜라이더를 꺼버립니다.
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = 5f;

        // n초 뒤에 메모리에서 삭제 (최적화)
        Destroy(gameObject, 3f);
    }
}