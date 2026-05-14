
public enum EUILifetimePolicy
{
    // 게임 시작 시 미리 로드하고 런타임 내내 유지합니다.
    Resident = 0,
    // 처음 표시할 때 로드하고, 닫은 뒤에도 캐시된 인스턴스를 유지합니다.
    Cached = 1,
    // 표시할 때 로드하고, 닫을 때 인스턴스와 에셋을 해제할 수 있습니다.
    Transient = 2,
}

public enum ESceneUIType
{
    None = 0,

    Title,
    CharacterSelect,
    InGameHUD

}


public enum EPopupUIType
{
    None = 0,

    Pause,
    LevelUp,
    SelectCard,
    GameOver
}