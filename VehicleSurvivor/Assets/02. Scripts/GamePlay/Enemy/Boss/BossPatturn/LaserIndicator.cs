using System.Collections;
using UnityEngine;

public class LaserIndicator : MonoBehaviour
{
    [Header("Meshes")]
    public Transform warningCylinderMesh;   // 수축할 바깥쪽 원통 메쉬
    public Transform laserCoreMesh;         // 굵기가 고정된 안쪽 진한 선 메쉬

    private float laserLength;

    // 보스가 이 인디케이터를 생성할 때 초기값들을 넘겨줍니다.
    public void Initialize(float startRadius, float endRadius, float coreRadius, float length, float duration)
    {
        this.laserLength = length;

        // 1. 코어 메쉬(중심선)는 수축하지 않고 처음부터 끝까지 얇은 굵기로 고정
        SetCylinderScale(laserCoreMesh, coreRadius);

        // 2. 바깥쪽 메쉬 수축 코루틴 시작
        StartCoroutine(ShrinkRoutine(startRadius, endRadius, duration));
    }

    private IEnumerator ShrinkRoutine(float startR, float endR, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float currentRadius = Mathf.Lerp(startR, endR, timer / duration);
            SetCylinderScale(warningCylinderMesh, currentRadius);
            yield return null;
        }
    }

    // 부모 스케일 오차 보정 기능을 포함한 통합 스케일 조절 함수
    private void SetCylinderScale(Transform targetMesh, float radius)
    {
        if (targetMesh == null) return;

        // X축 90도 회전된 메쉬 기준으로 X, Z가 굵기, Y가 길이입니다.
        if (targetMesh.parent != null)
        {
            Vector3 parentScale = targetMesh.parent.lossyScale;
            targetMesh.localScale = new Vector3(
                radius / parentScale.x,
                (laserLength / 2f) / parentScale.y,
                radius / parentScale.z
            );
        }
        else
        {
            targetMesh.localScale = new Vector3(radius, laserLength / 2f, radius);
        }
    }
}