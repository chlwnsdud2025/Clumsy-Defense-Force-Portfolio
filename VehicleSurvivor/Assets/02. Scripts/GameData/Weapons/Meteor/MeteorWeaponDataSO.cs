using UnityEngine;

[CreateAssetMenu(
    fileName = "SO_MeteorWeapon",
    menuName = "Weapon/Meteor Weapon"
)]
public class MeteorWeaponDataSO : WeaponDataBaseSO
{
    [Header("메테오 프리팹")]
    public GameObject meteorPrefab;
}