using UnityEngine;

public abstract class WeaponDataBaseSO : ScriptableObject
{
    [Header("무기 기본 정보")]
    public string weaponID;

    public string weaponName;

    [TextArea]
    public string description;
}