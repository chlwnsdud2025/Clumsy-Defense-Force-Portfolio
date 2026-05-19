using UnityEngine;

[CreateAssetMenu(
    fileName = "SO_HomingWeapon",
    menuName = "Weapon/Homing Weapon"
)]
public class HomingWeaponDataSO : WeaponDataBaseSO
{
    [Header("유도탄 프리팹")]
    public GameObject homingProjectilePrefab;

    [Header("발사 이펙트")]
    public ParticleSystem shootParticlePrefab;

    [Header("히트 이펙트")]
    public GameObject hitEffectPrefab;
}