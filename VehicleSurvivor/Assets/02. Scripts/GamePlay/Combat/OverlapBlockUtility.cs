using UnityEngine;

public static class OverlapBlockUtility
{
    // 기존 코드와의 호환성을 위한 기본 함수 (높이 제한이 없는 무한 원기둥 버전)
    public static void ClipApproach(ref Vector3 velocity, Vector3 selfPosition, Vector3 otherPosition, float minDistance)
    {
        ClipApproach(ref velocity, selfPosition, otherPosition, minDistance, float.MaxValue);
    }

    public static void ClipApproach(ref Vector3 velocity, Vector3 selfPosition, Vector3 otherPosition, float minDistance, float maxHeightDifference)
    {
        // 1. [핵심 추가] 두 오브젝트의 Y축 고도 차이가 설정한 높이보다 크다면 충돌을 계산하지 않고 통과
        float yDiff = Mathf.Abs(otherPosition.y - selfPosition.y);
        if (yDiff >= maxHeightDifference)
            return;

        // 2. 기존 XZ 평면 밀어내기 로직
        Vector3 toOther = otherPosition - selfPosition;
        toOther.y = 0f;

        float sqrMinDist = minDistance * minDistance;
        if (toOther.sqrMagnitude >= sqrMinDist)
            return;

        Vector3 awayFromOther = -toOther.normalized;
        float approach = Vector3.Dot(velocity, -awayFromOther);
        if (approach > 0f)
            velocity += awayFromOther * approach;
    }
}