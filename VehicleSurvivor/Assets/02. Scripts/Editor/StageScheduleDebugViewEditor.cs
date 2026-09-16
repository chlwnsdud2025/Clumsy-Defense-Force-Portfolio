using UnityEditor;
using UnityEngine;

/// <summary>
/// StageScheduleDebugView 커스텀 인스펙터.
/// 페이즈 구성 막대와 웨이브 스케줄(타임라인 + 표)을 그리고, 캡처 창을 여는 버튼을 제공한다.
/// </summary>
[CustomEditor(typeof(StageScheduleDebugView))]
public class StageScheduleDebugViewEditor : Editor
{
    private readonly StageScheduleDrawer.Context context = new StageScheduleDrawer.Context();

    public override bool RequiresConstantRepaint() => Application.isPlaying;

    public override void OnInspectorGUI()
    {
        StageScheduleDebugView view = (StageScheduleDebugView)target;
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("stageRunManager"));

        StageRunManager manager = view.ResolveManager();
        StageTheme_SO theme = manager != null ? manager.currentTheme : null;

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField("Theme (from manager)", theme, typeof(StageTheme_SO), false);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("captureMode"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("showEnemyNames"));
        serializedObject.ApplyModifiedProperties();

        if (manager == null)
        {
            EditorGUILayout.HelpBox("씬에서 StageRunManager를 찾지 못했습니다. 필드에 직접 지정하세요.", MessageType.Warning);
            return;
        }

        if (theme == null)
        {
            EditorGUILayout.HelpBox("StageRunManager.currentTheme이 비어 있습니다.", MessageType.Warning);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("캡처 창 열기 (둘 다)"))
                StageScheduleCaptureWindow.Open(view, StageScheduleCaptureWindow.Mode.Both);
            if (GUILayout.Button("페이즈 구성만"))
                StageScheduleCaptureWindow.Open(view, StageScheduleCaptureWindow.Mode.Phases);
            if (GUILayout.Button("웨이브 스케줄만"))
                StageScheduleCaptureWindow.Open(view, StageScheduleCaptureWindow.Mode.Waves);
        }

        context.theme = theme;
        context.manager = manager;
        context.palette = EditorGUIUtility.isProSkin ? StageScheduleDrawer.Palette.Dark : StageScheduleDrawer.Palette.Light;
        context.scale = view.captureMode ? 1.25f : 1f;
        context.captureMode = view.captureMode;
        context.showEnemyNames = view.showEnemyNames;
        context.RefreshRuntime();

        EditorGUILayout.Space(8f);
        StageScheduleDrawer.DrawPhaseSection(context);

        EditorGUILayout.Space(12f);
        int selected = view.selectedPhase;
        StageScheduleDrawer.DrawWaveSection(context, ref selected, !view.captureMode);
        if (selected != view.selectedPhase)
        {
            Undo.RecordObject(view, "Select Phase");
            view.selectedPhase = selected;
            EditorUtility.SetDirty(view);
        }
    }

    [MenuItem("GameObject/Survivor/Stage Schedule Debug View", false, 10)]
    private static void CreateDebugView()
    {
        GameObject go = new GameObject("StageScheduleDebugView");
        go.AddComponent<StageScheduleDebugView>();
        Undo.RegisterCreatedObjectUndo(go, "Create Stage Schedule Debug View");
        Selection.activeGameObject = go;
    }
}
