using System;
using UnityEngine;

[Serializable] //이 클래스를 Unity 인스펙터에서 보이게 하고, 저장 가능하게 만드는 표시
public class WeaponLevelData
{
    // TODO : 스탯 관련 수정
    [Header("공통 스탯")]
    //public float damage = 10f;
    //public float cooldown = 1f;

    [Header("투사체 스탯")]
    public float bulletSpeed = 20f;
    //public int projectileCount = 1;
    public float spreadAngle = 15f;

    [Header("범위 스탯")]
    public float radius = 5f;

    [Header("체인 번개")]
    public int chainCount = 3;
    public float chainRadius = 5f;

    [Header("메테오")]
    public float explosionRadius = 5f;
    public float meteorDelay = 1f;
    //-------------------------------------

    [Header("UI")]
    [TextArea] //string 입력칸을 여러 줄 입력 가능한 텍스트 박스로 보여주는 속성
    [SerializeField] private string description;


    [Header("Combat")]
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

    public string Description => description;

    public float Cooldown => cooldown;
    public float Damage => damage;
    public float ProjectileSpeed => projectileSpeed;
    public float Duration => duration;
    public float Size => size;
    public int ProjectileCount => projectileCount;
    public float TickInterval => tickInterval;
    public float Range => range;

}