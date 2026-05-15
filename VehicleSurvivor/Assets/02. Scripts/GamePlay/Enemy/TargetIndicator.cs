using System.Collections;
using UnityEngine;

public class TargetIndicator : MonoBehaviour
{
    public void Initialize(float radius, float duration)
    {
        StartCoroutine(GrowAndDestroy(radius, duration));
    }

    private IEnumerator GrowAndDestroy(float radius, float duration)
    {
        float timer = 0f;
        transform.localScale = Vector3.zero;

        // 반지름의 2배가 전체 지름(Scale)
        float targetSize = radius * 2f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float currentSize = Mathf.Lerp(0f, targetSize, timer / duration);
            transform.localScale = new Vector3(currentSize, currentSize, currentSize);
            yield return null;
        }

        // 투사체 착탄(폭발) 후 자연스럽게 사라지도록 약간의 대기 시간 추가
        yield return new WaitForSeconds(0.1f);
        Destroy(gameObject);
    }
}