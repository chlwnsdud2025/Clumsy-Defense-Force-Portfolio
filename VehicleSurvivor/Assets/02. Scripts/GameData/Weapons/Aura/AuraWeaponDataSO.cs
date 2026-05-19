using UnityEngine;

[CreateAssetMenu(
    fileName = "SO_AuraWeapon",
    menuName = "Weapon/Aura Weapon"
)]
public class AuraWeaponDataSO : WeaponDataBaseSO
{
    [Header("오라 이펙트")]
    public GameObject auraEffectPrefab;
}