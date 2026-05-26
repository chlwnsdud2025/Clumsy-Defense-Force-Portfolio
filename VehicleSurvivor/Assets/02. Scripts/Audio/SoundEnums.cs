public enum ESoundId
{
    None = 0,

    SFX_Player_Footstep,
    SFX_Player_Land,
    SFX_Player_Hurt,
    SFX_Player_Death,
    SFX_Player_Heal,

    SFX_Pickup_Exp,
    SFX_Pickup_Gold,
    SFX_Pickup_Collect,

    SFX_Weapon_Fire,
    SFX_Weapon_Hit,
    SFX_Weapon_Explosion,

    SFX_Enemy_Hit,
    SFX_Enemy_Death,

    SFX_Boss_Appear,
    SFX_Boss_Telegraph,
    SFX_Boss_Attack,
    SFX_Boss_Defeat,

    SFX_UI_Click,
    SFX_LevelUp,
    SFX_Reward_Select,
    SFX_Reward_Apply,
    SFX_Chest_Open,
    SFX_Wave_Nightmare,
    SFX_Stage_Clear,
    SFX_GameOver,

    SFX_Env_Portal,
    SFX_Env_JumpPad,
}

public enum EBgmId
{
    None = 0,

    BGM_Title,
    BGM_InGame_Normal,
    BGM_InGame_Nightmare,
    BGM_Hub,
    BGM_Boss,
    BGM_Reward,
    BGM_GameOver,
}

public enum ESfxPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
}
