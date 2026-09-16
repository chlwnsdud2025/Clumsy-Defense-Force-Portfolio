using UnityEditor;
using UnityEngine;

/// <summary>
/// 스테이지 스케줄 캡처 창. 고정 폭으로 페이즈 구성 / 웨이브 스케줄을 그려
/// 포트폴리오 이미지로 캡처하기 좋은 구도를 만든다. 툴바를 숨기면 내용만 남는다.
/// </summary>
public class StageScheduleCaptureWindow : EditorWindow
{
    public enum Mode { Both, Phases, Waves }

    private const float FixedWidth = 1200f;
    private const float Padding = 24f;

    private StageScheduleDebugView view;
    private Mode mode = Mode.Both;
    private bool lightBackground = true;
    private bool hideToolbar;
    private bool showEnemyNames = true;
    private float scale = 1.4f;
    private int selectedPhase;
    private float contentHeight = 400f;

    private readonly StageScheduleDrawer.Context context = new StageScheduleDrawer.Context();

    public static void Open(StageScheduleDebugView view, Mode mode)
    {
        StageScheduleCaptureWindow window = GetWindow<StageScheduleCaptureWindow>(true, "Stage Schedule Capture", true);
        window.view = view;
        window.mode = mode;
        window.selectedPhase = view != null ? view.selectedPhase : 0;
        window.showEnemyNames = view == null || view.showEnemyNames;
        window.ApplySize();
        window.Show();
    }

    private void OnInspectorUpdate()
    {
        if (Application.isPlaying)
            Repaint();
    }

    private void ApplySize()
    {
        float h = Mathf.Clamp(contentHeight, 160f, 1400f);
        minSize = new Vector2(FixedWidth, h);
        maxSize = new Vector2(FixedWidth, h);
    }

    private void OnGUI()
    {
        HandleEscape();

        StageRunManager manager = view != null ? view.ResolveManager() : null;
        StageTheme_SO theme = manager != null ? manager.currentTheme : null;

        context.theme = theme;
        context.manager = manager;
        context.palette = lightBackground ? StageScheduleDrawer.Palette.Light : StageScheduleDrawer.Palette.Dark;
        context.scale = scale;
        context.captureMode = true;
        context.showEnemyNames = showEnemyNames;
        context.RefreshRuntime();

        EditorGUI.DrawRect(new Rect(0f, 0f, position.width, position.height), context.palette.bg);

        if (!hideToolbar)
            DrawToolbar();

        if (view == null || theme == null)
        {
            EditorGUILayout.HelpBox("StageScheduleDebugView 또는 StageTheme_SO를 찾을 수 없습니다. 인스펙터에서 다시 여세요.", MessageType.Warning);
            return;
        }

        // 내용 영역: 고정 폭 안쪽에 여백을 두고 그린다. 높이는 실제 그린 만큼 측정해 창 크기에 반영.
        GUILayout.BeginVertical();
        GUILayout.Space(Padding);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(Padding);
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(FixedWidth - Padding * 2f)))
            {
                if (mode != Mode.Waves)
                    StageScheduleDrawer.DrawPhaseSection(context);

                if (mode == Mode.Both)
                    GUILayout.Space(22f * scale);

                if (mode != Mode.Phases)
                {
                    StageScheduleDrawer.DrawWaveSection(context, ref selectedPhase, false);
                    if (view.selectedPhase != selectedPhase)
                        view.selectedPhase = selectedPhase;
                }
            }
            GUILayout.Space(Padding);
        }
        GUILayout.Space(Padding);
        GUILayout.EndVertical();

        if (Event.current.type == EventType.Repaint)
        {
            float measured = GUILayoutUtility.GetLastRect().yMax;
            if (Mathf.Abs(measured - contentHeight) > 1f)
            {
                contentHeight = measured;
                ApplySize();
            }
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            mode = (Mode)EditorGUILayout.EnumPopup(mode, EditorStyles.toolbarPopup, GUILayout.Width(90f));

            StageTheme_SO theme = context.theme;
            if (mode != Mode.Phases && theme != null && theme.phases != null && theme.phases.Count > 0)
            {
                string[] labels = new string[theme.phases.Count];
                for (int i = 0; i < labels.Length; i++) labels[i] = $"Phase {i + 1}";
                selectedPhase = EditorGUILayout.Popup(Mathf.Clamp(selectedPhase, 0, labels.Length - 1), labels, EditorStyles.toolbarPopup, GUILayout.Width(90f));
            }

            GUILayout.Space(8f);
            lightBackground = GUILayout.Toggle(lightBackground, "밝은 배경", EditorStyles.toolbarButton, GUILayout.Width(70f));
            showEnemyNames = GUILayout.Toggle(showEnemyNames, "적 구성", EditorStyles.toolbarButton, GUILayout.Width(60f));

            GUILayout.Space(8f);
            GUILayout.Label("글씨", GUILayout.Width(30f));
            scale = GUILayout.HorizontalSlider(scale, 1f, 2f, GUILayout.Width(100f));
            GUILayout.Label($"{scale:0.0}x", GUILayout.Width(36f));

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("툴바 숨기기 (Esc로 복귀)", EditorStyles.toolbarButton))
                hideToolbar = true;
        }
    }

    // 툴바를 숨긴 상태에서 Esc를 누르면 복귀
    private void HandleEscape()
    {
        Event e = Event.current;
        if (hideToolbar && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            hideToolbar = false;
            e.Use();
            Repaint();
        }
    }
}
