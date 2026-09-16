using UnityEngine;

/// <summary>
/// 디버그·캡처용 컴포넌트. 씬의 StageRunManager가 사용하는 스테이지 스케줄
/// (페이즈 구성 · 웨이브 스케줄)을 인스펙터에 시각화한다. 게임 로직은 없다.
/// 빈 오브젝트에 붙여 두고 인스펙터 또는 "캡처 창 열기"로 확인한다.
/// </summary>
public class StageScheduleDebugView : MonoBehaviour
{
    [Tooltip("비워두면 씬에서 자동으로 찾습니다.")]
    public StageRunManager stageRunManager;

    [Header("View")]
    [Tooltip("웨이브 스케줄에 표시할 페이즈 (0부터)")]
    public int selectedPhase = 0;

    [Tooltip("캡처 모드: 보조 UI를 숨기고 글씨를 키웁니다.")]
    public bool captureMode = false;

    [Tooltip("웨이브 표에 적 구성(풀 이름)을 표시합니다.")]
    public bool showEnemyNames = true;

    /// <summary>StageRunManager를 찾아 반환한다 (플레이 중이면 싱글턴, 아니면 씬 검색).</summary>
    public StageRunManager ResolveManager()
    {
        if (stageRunManager != null)
            return stageRunManager;

        if (Application.isPlaying && StageRunManager.TryGetInstance(out StageRunManager instance))
            stageRunManager = instance;
        else
            stageRunManager = FindAnyObjectByType<StageRunManager>(FindObjectsInactive.Include);

        return stageRunManager;
    }

    public StageTheme_SO Theme
    {
        get
        {
            StageRunManager manager = ResolveManager();
            return manager != null ? manager.currentTheme : null;
        }
    }
}
