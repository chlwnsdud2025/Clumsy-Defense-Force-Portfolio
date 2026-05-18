using UnityEngine;

[CreateAssetMenu(
    fileName = "SO_WeaponData",
    menuName = "Weapon/Weapon Data"
)]
public class WeaponDataSO : ScriptableObject
{
    [Header("무기 기본 정보")]
    [Tooltip("무기 고유 ID. 예: carrot_cannon, shell_spin, forest_friend")]
    public string weaponID;

    [Tooltip("인게임 표시 이름. 예: 당근 캐논, 쉘 스핀")]
    public string weaponName;

    [TextArea]
    [Tooltip("무기 설명")]
    public string description;


    [Header("무기 분류")]
    [Tooltip("캐릭터 고유무기인지 공용무기인지 구분용")]
    public WeaponCategory weaponCategory;

    [Tooltip("무기 공격 방식 구분용")]
    public WeaponAttackType attackType;


    [Header("진화 정보")]
    [Tooltip("진화 전 기본 무기인지 여부")]
    public bool canEvolve = true;

    [Tooltip("진화 무기 이름")]
    public string evolutionWeaponName;

    [Tooltip("진화 조건 설명. 예: 공격력 증가, 체력 증가")]
    public string evolutionCondition;


    [Header("레벨 데이터")]
    [Tooltip("Lv1부터 순서대로 입력")]
    public WeaponLevelData[] levelData;
}

public enum WeaponCategory
{
    CharacterUnique,
    Common
}

public enum WeaponAttackType
{
    Projectile,
    Orbit,
    GroundDot,
    Drone,
    Meteor,
    Laser,
    Shockwave,
    Aura,
    ChainLightning,
    Boomerang,
    BouncyProjectile,
    Sweep,
    FullScreenExplosion,
    Shield,
    Trap,
    Swarm
}