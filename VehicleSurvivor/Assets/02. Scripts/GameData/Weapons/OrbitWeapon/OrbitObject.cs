using System.Collections.Generic;
using UnityEngine;

public class OrbitObject : MonoBehaviour
{
    // 변경된 DamageInfo 적용
    private DamageInfo damageInfo;
    private float hitCooldown;

    private readonly Dictionary<IDamageable, float> hitTimers =
        new Dictionary<IDamageable, float>();

    public void Initialize(
        DamageInfo newDamageInfo,
        float newHitCooldown
    )
    {
        damageInfo = newDamageInfo;
        hitCooldown = Mathf.Max(0.05f, newHitCooldown);
    }

    private void OnTriggerStay(Collider other)
    {
        // [수정] 플레이어 레이어 제외
        if (IsPlayerLayer(other))
            return;

        IDamageable damageable =
            other.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        if (hitTimers.ContainsKey(damageable))
        {
            if (Time.time < hitTimers[damageable])
                return;

            hitTimers[damageable] =
                Time.time + hitCooldown;
        }
        else
        {
            hitTimers.Add(
                damageable,
                Time.time + hitCooldown
            );
        }

        // DamageInfo 객체로 데미지 전달
        damageable.TakeDamage(damageInfo);

        Debug.Log($"[OrbitObject] 데미지: {damageInfo.Amount}");
    }

    private void OnTriggerExit(Collider other)
    {
        // [수정] 플레이어 레이어 제외 (불필요한 연산 방지)
        if (IsPlayerLayer(other))
            return;

        IDamageable damageable =
            other.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        if (hitTimers.ContainsKey(damageable))
        {
            hitTimers.Remove(damageable);
        }
    }

    // [추가] 플레이어 판별 로직
    private bool IsPlayerLayer(Collider other)
    {
        int playerLayer = LayerMask.NameToLayer("Player");

        if (playerLayer == -1)
            return false;

        if (other.gameObject.layer == playerLayer)
            return true;

        Transform root = other.transform.root;
        return root.gameObject.layer == playerLayer;
    }
}