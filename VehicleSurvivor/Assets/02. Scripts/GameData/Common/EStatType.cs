public enum EStatType
{
    None = 0,

    // 생존 (100~)
    MaxHealth = 100,                  // 최대 체력
    HealthRegen = 101,                // 체력 회복 (초당)
    Defense = 102,                    // 방어력

    // 이동 (200~)
    MoveSpeedMultiplier = 200,        // 이동 속도 배율

    // 공격 (300~)
    DamageMultiplier = 300,           // 피해 배율 (무기 base × 배율, 실제 피해 공식 적용)
    MagicDamageMultiplier = 301,      // 마법 무기 추가 피해 배율 (enum/SO 예약, 피해 공식 미적용)
    EvolvedWeaponDamage = 302,        // 진화 무기 추가 피해 배율 (진화 무기에만 DamageMultiplier 와 곱연산)

    // 치명타 (400~)
    CriticalChance = 400,             // 치명타 확률
    CriticalDamageMultiplier = 401,   // 치명타 피해 배율

    // 무기 (500~)
    CooldownReduction = 500,          // 무기 쿨타임 감소
    ProjectileCount = 501,            // 투사체 개수
    WeaponSizeMultiplier = 502,       // 무기 크기/범위 배율

    // 보상 (600~) — 런 진행·자원·편의·확률
    ExpGainMultiplier = 600,          // 경험치 획득 배율
    GoldGainMultiplier = 601,           // 골드 획득 배율
    Luck = 602,                         // 행운 (상자 희귀도 등 확률 보정)
    PickupRangeMultiplier = 603,        // 아이템 획득 범위 배율
}
