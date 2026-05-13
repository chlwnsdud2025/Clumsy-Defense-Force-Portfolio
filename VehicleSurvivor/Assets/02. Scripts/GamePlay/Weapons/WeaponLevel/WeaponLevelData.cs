using UnityEngine;

[System.Serializable]
public class WeaponLevelData
{
    [Header("공통 스탯")]
    public float damage = 10f;
    public float cooldown = 1f;

    [Header("투사체 스탯")]
    public float bulletSpeed = 20f;
    public int projectileCount = 1;
    public float spreadAngle = 15f;

    [Header("범위 스탯")]
    public float radius = 5f;

    [Header("체인 번개")]
    public int chainCount = 3;
    public float chainRadius = 5f;

    [Header("메테오")]
    public float explosionRadius = 5f;
    public float meteorDelay = 1f;
}