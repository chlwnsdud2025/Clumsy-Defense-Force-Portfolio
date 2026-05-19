using UnityEngine;

[CreateAssetMenu(
    fileName = "SO_ChainLightningWeapon",
    menuName = "Weapon/Chain Lightning Weapon"
)]
public class ChainLightningWeaponDataSO : WeaponDataBaseSO
{
    [Header("번개 이펙트")]
    public GameObject lightningEffectPrefab;
}