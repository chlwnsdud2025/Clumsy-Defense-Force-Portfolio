using UnityEngine;

public static class OverlapBlockUtility
{
    public static void ClipApproach(ref Vector3 velocity, Vector3 selfPosition, Vector3 otherPosition, float minDistance)
    {
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
