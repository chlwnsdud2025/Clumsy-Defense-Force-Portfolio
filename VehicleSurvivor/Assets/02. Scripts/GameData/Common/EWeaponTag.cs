using System;

[Flags]
public enum EWeaponTag
{
    None = 0,

    Projectile = 1 << 0,      // 투사체
    Area = 1 << 1,            // 장판
    Range = 1 << 2,           // 범위
    Shield = 1 << 3,          // 보호막
    Electric = 1 << 4,        // 전기
    Explosion = 1 << 5,       // 폭발
    Growth = 1 << 6,          // 성장
    Reward = 1 << 7,          // 보상
    Magic = 1 << 8,           // 마법
    Movement = 1 << 9,        // 이동

    SingleTarget = 1 << 10,
    Rotation = 1 << 11,
    Summon = 1 << 12,
    AutoAttack = 1 << 13,
    Fire = 1 << 14,
    Beam = 1 << 15,
    Pierce = 1 << 16,
    Chain = 1 << 17,
}
