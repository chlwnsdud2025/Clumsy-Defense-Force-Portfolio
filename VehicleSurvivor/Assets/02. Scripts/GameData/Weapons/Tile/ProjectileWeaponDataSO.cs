using UnityEngine;

[CreateAssetMenu(
    fileName = "SO_ProjectileWeapon",
    menuName = "Weapon/Projectile Weapon"
)]
public class ProjectileWeaponDataSO : WeaponDataBaseSO
{
    [Header("투사체 설정")]
    public GameObject projectilePrefab;

    [Header("발사 이펙트")]
    public ParticleSystem shootParticlePrefab;
}   