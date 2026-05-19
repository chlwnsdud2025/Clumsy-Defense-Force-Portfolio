using UnityEngine;

[CreateAssetMenu(
    fileName = "SO_OrbitWeapon",
    menuName = "Weapon/Orbit Weapon"
)]
public class OrbitWeaponDataSO : WeaponDataBaseSO
{
    [Header("회전 오브젝트 프리팹")]
    public GameObject orbitPrefab;

    [Header("히트 이펙트")]
    public GameObject hitEffectPrefab;
}