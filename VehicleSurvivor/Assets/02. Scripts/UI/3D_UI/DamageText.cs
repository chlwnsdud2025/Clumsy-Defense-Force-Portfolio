using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위한 네임스페이스

public class DamageText : MonoBehaviour
{
    public TextMeshPro textMesh;
    public float lifeTime = 0.5f; // 텍스트가 떠 있을 시간

    // 몬스터가 맞았을 때 외부에서 이 함수를 호출해줄 겁니다.
    public void Setup(float damage, Vector3 hitPosition)
    {
        // 1. 위치와 텍스트 내용 설정
        transform.position = hitPosition;
        textMesh.text = damage.ToString();

        // 2. lifeTime 초 뒤에 Hide 함수 실행 (임시 타이머)
        Invoke("Hide", lifeTime);
    }

    private void Hide()
    {
        // 내 할 일을 다 했으니 풀 매니저에게 나를 반납함
        DamageTextPool.Instance.ReturnToPool(this.gameObject);
    }

    // 예외 처리: 게임 오브젝트가 비활성화될 때 혹시 모를 Invoke 예약 취소
    private void OnDisable()
    {
        CancelInvoke();
    }
}