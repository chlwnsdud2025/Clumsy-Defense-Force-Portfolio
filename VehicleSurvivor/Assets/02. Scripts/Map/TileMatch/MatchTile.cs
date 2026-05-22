using UnityEngine;
using System.Collections;

public class MatchTile : MonoBehaviour
{
    public Color myColor; // 이 타일의 진짜 색상
    private Color defaultColor = Color.gray; // 평소에 숨길 때의 색상

    private Renderer _renderer;
    private Collider _collider;
    private Vector3 _startPos;
    private bool _isDropped = false;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _collider = GetComponent<Collider>();
        // 생성된 직후의 원래 위치를 기억합니다.
        _startPos = transform.position;
    }

    public void SetColor(Color c)
    {
        myColor = c;
        ShowColor(); // 처음 지정될 땐 색상을 보여줌
    }

    public void ShowColor() { _renderer.material.color = myColor; }
    public void HideColor() { _renderer.material.color = defaultColor; }

    public void Drop()
    {
        if (_isDropped) return;
        _isDropped = true;
        _collider.enabled = false; // 플레이어가 빠지도록 콜라이더 끄기
        StartCoroutine(DropAnim());
    }

    private IEnumerator DropAnim()
    {
        float speed = 15f;
        // 아래로 10만큼 떨어질 때까지 스르륵 내려감
        while (transform.position.y > _startPos.y - 30f)
        {
            transform.Translate(Vector3.down * speed * Time.deltaTime, Space.World);
            yield return null;
        }
    }

    public void ResetTile()
    {
        StopAllCoroutines(); // 떨어지던 중이었다면 멈춤
        _isDropped = false;
        _collider.enabled = true;
        transform.position = _startPos; // 원래 위치로 순간이동
        HideColor(); // 다시 회색으로 덮음
    }
}