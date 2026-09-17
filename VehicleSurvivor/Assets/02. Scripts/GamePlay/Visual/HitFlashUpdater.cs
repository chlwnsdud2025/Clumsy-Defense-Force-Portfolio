using UnityEngine;

// 적/보스마다 LateUpdate를 두지 않고, 현재 피격 연출 중인 개체만 한 곳에서 갱신한다.
public sealed class HitFlashUpdater : MonoBehaviour
{
    private void LateUpdate() => HitFlashEffect.UpdateActiveFlashes(Time.deltaTime);
}
