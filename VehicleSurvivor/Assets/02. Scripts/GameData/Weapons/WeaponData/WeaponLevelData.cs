using System;
using UnityEngine;

[Serializable]
public class WeaponLevelData
{
    [Header("UI")]
    [TextArea]
    [SerializeField] private string description;

    [Header("Common Combat")]
    [Min(0.1f)]
    [SerializeField] private float cooldown = 1f;

    [Min(1f)]
    [SerializeField] private float damage = 10f;

    [Min(0.1f)]
    [SerializeField] private float projectileSpeed = 10f;

    [Min(0.1f)]
    [SerializeField] private float duration = 1f;

    [Min(0.1f)]
    [SerializeField] private float size = 1f;

    [Min(1)]
    [SerializeField] private int projectileCount = 1;

    [Min(0.1f)]
    [SerializeField] private float tickInterval = 0.5f;

    [Min(0.1f)]
    [SerializeField] private float range = 1f;

    [Header("Projectile")]
    [Min(0.1f)]
    [SerializeField] private float bulletSpeed = 20f;

    [SerializeField] private float spreadAngle = 15f;

    [Min(0.1f)]
    [SerializeField] private float radius = 5f;

    [Min(0)]
    [SerializeField] private int bounceCount = 3;

    [Min(0)]
    [SerializeField] private int pierceCount = 0;

    [Header("Chain Lightning")]
    [Min(0)]
    [SerializeField] private int chainCount = 3;

    [Min(0.1f)]
    [SerializeField] private float chainRadius = 5f;

    [Header("Meteor")]
    [Min(0.1f)]
    [SerializeField] private float explosionRadius = 5f;

    [Min(0f)]
    [SerializeField] private float meteorDelay = 1f;

    [Header("Ground Dot")]
    [Min(0.1f)]
    [SerializeField] private float dotDuration = 3f;

    [Min(0.1f)]
    [SerializeField] private float dotTickInterval = 0.5f;

    [Min(0.1f)]
    [SerializeField] private float dotWidth = 2f;

    [Min(0.1f)]
    [SerializeField] private float dotLength = 5f;

    [Header("Shockwave")]
    [Min(0.1f)]
    [SerializeField] private float shockwaveRange = 5f;

    [Range(0f, 360f)]
    [SerializeField] private float shockwaveAngle = 90f;

    public string Description => description;

    public float Cooldown => cooldown;
    public float Damage => damage;
    public float ProjectileSpeed => projectileSpeed;
    public float Duration => duration;
    public float Size => size;
    public int ProjectileCount => projectileCount;
    public float TickInterval => tickInterval;
    public float Range => range;
    public float BulletSpeed => bulletSpeed;
    public float SpreadAngle => spreadAngle;
    public float Radius => radius;
    public int BounceCount => bounceCount;
    public int ChainCount => chainCount;
    public float ChainRadius => chainRadius;
    public float ExplosionRadius => explosionRadius;
    public float MeteorDelay => meteorDelay;
    public float DotDuration => dotDuration;
    public float DotTickInterval => dotTickInterval;
    public float DotWidth => dotWidth;
    public float DotLength => dotLength;
    public float ShockwaveRange => shockwaveRange;
    public float ShockwaveAngle => shockwaveAngle;

    public int PierceCount => pierceCount;
}
