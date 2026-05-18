using UnityEngine;

[System.Serializable]
public class WeaponLevelData
{
    [Header("공통 스탯")]
    [Tooltip("대부분의 무기 공통 사용: 투사체, 오라, 메테오, 드론, 체인, 충격파, 장판 등")]
    public float damage = 10f;

    [Tooltip("공격 주기. 대부분의 무기 공통 사용")]
    public float cooldown = 1f;


    [Header("투사체 스탯")]
    [Tooltip("Projectile, Drone, Boomerang, Bouncy, Sweep 등에 사용")]
    public float bulletSpeed = 20f;

    [Tooltip("Projectile 탄 수 / Orbit 개수 / Drone 개수 / Laser 개수 등에 사용")]
    public int projectileCount = 1;

    [Tooltip("Projectile 산탄 각도 / Twinkle Shot 같은 원뿔 발사에 사용")]
    public float spreadAngle = 15f;

    [Tooltip("Projectile 관통 횟수 / 당근 캐논 진화, 종이비행기 진화 등에 사용")]
    public int pierceCount = 0;

    [Tooltip("BouncyProjectile 탱탱볼 튕김 횟수에 사용")]
    public int bounceCount = 0;


    [Header("범위 스탯")]
    [Tooltip("Aura 범위 / Orbit 반경 / Drone 배치 거리 / Global 범위 등에 사용")]
    public float radius = 5f;

    [Tooltip("Meteor, GiftBomb, GummyBearDrop 폭발 범위에 사용")]
    public float explosionRadius = 5f;


    [Header("체인 번개 스탯")]
    [Tooltip("ChainLightning 연쇄 횟수 / 정전기 진화 등에 사용")]
    public int chainCount = 3;

    [Tooltip("ChainLightning 다음 적 탐색 범위에 사용")]
    public float chainRadius = 5f;


    [Header("메테오 / 낙하 스탯")]
    [Tooltip("Meteor, GummyBearDrop, ToySatellite 낙하 전 대기 시간에 사용")]
    public float meteorDelay = 0.3f;


    [Header("충격파 스탯")]
    [Tooltip("Shockwave, DinoRoar, BearPawShockwave 전방 공격 거리에 사용")]
    public float shockwaveRange = 6f;

    [Tooltip("Shockwave, DinoRoar 전방 부채꼴 각도에 사용")]
    public float shockwaveAngle = 90f;

    [Tooltip("Shockwave 진화, DinoRoar 진화의 넉백 힘에 사용")]
    public float knockbackPower = 0f;

    [Tooltip("DinoRoar 진화 등 기절 시간에 사용")]
    public float stunDuration = 0f;


    [Header("장판 / 지속 피해 스탯")]
    [Tooltip("GroundDot, FireLine, BlockTrap, Yoyo 진화 장판 등에 사용")]
    public float dotDuration = 4f;

    [Tooltip("GroundDot, Aura류 지속 피해 간격에 사용")]
    public float dotTickInterval = 0.5f;

    [Tooltip("FireLine, BlockTrap 장판 너비에 사용")]
    public float dotWidth = 2f;

    [Tooltip("FireLine, BlockTrap 장판 길이에 사용")]
    public float dotLength = 6f;

    [Tooltip("BlockTrap 진화, FireLine 진화 등 이동속도 감소 비율에 사용")]
    public float slowRate = 0f;


    [Header("보호막 스탯")]
    [Tooltip("ShieldWeapon, SoapBubble 보호막 체력에 사용")]
    public float shieldAmount = 0f;

    [Tooltip("SoapBubble 반격 데미지에 사용")]
    public float counterDamage = 0f;

    [Tooltip("Shield 유지 시간 또는 재생 쿨타임에 사용")]
    public float duration = 0f;


    [Header("전체 공격 스탯")]
    [Tooltip("FullScreenExplosion, BigFirework 정예/보스 추가 피해에 사용")]
    public float eliteDamageMultiplier = 1f;

    [Tooltip("BigFirework 진화 경험치 보너스에 사용")]
    public float bonusExpRate = 0f;

    [Tooltip("BigFirework 진화 골드 드랍 확률에 사용")]
    public float goldDropChance = 0f;


    [Header("진화 / 특수 확률")]
    [Tooltip("FireLine 진화 폭발 확률 / Aura 수면 확률 등에 사용")]
    public float effectChance = 0f;

    [Tooltip("치명타 확률 조건 무기나 탱탱볼 진화 등에 사용")]
    public float critChance = 0f;

    [Tooltip("치명타 피해 배율에 사용")]
    public float critMultiplier = 1.5f;
}