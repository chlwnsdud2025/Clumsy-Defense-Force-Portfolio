using UnityEngine;

[System.Serializable]
public class WeaponLevelData
{
    [Header("기본 공격 정보")]
    public int damage = 10;
    public float cooldown = 1f;
    public float range = 5f;

    [Header("파티클 / 이펙트")]
    public GameObject attackParticlePrefab;

    [Header("투사체 전용")]
    public float projectileSpeed = 10f;
    public int projectileCount = 1;

    [Header("특수 옵션")]
    public bool canPierce;
    public int pierceCount = 0;

    public bool canExplode;
    public float explosionRadius = 0f;
}