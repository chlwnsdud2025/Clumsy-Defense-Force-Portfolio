using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// [에디터 전용] Play 중 씬뷰에서 Enemy를 클릭(선택)하면 EnemyManager의 공간분할 그리드가
/// 해당 Enemy 주변 이웃을 어떻게 탐색하는지 씬뷰에 그려줍니다.
/// 게임 빌드에는 포함되지 않습니다.
///
/// 색상 의미:
///   연초록 셀(굵음) : 선택한 Enemy가 속한 셀
///   초록 셀         : 탐색 대상 3x3 이웃 셀 (실제 계산 범위)
///   흰색 격자       : 탐색하지 않는 셀 (그리드 격자 / 점유 셀 / 바깥 Enemy)
///   연두 원         : separationRadius
///   빨간 선         : 반경 안에서 실제로 감지된 Enemy (분리 힘 계산에 사용)
///   노란 선         : 3x3 셀 안에 있어 거리 검사는 했지만 반경 밖이라 제외된 Enemy
///   보라 점         : MaxChecks 초과로 검사가 생략된 Enemy
/// </summary>
[InitializeOnLoad]
public static class EnemySpatialGridGizmo
{
    // 설정 (필요하면 여기 값만 바꾸면 됩니다)
    private static int gridDrawRange = 4;
    private static float groundOffset = 0.05f;
    private static float lineWidth = 6f;
    private static float thinLineWidth = 3f;
    private static float markerRadius = 0.35f;
    private static bool showStatsPanel = true;
    private static bool showCellLabels = true;
    private static bool showDistanceLabels = true;

    private static readonly Color centerCellColor = new Color(0.4f, 1f, 0.4f, 1f);
    private static readonly Color neighborCellColor = new Color(0.1f, 0.9f, 0.3f, 1f);
    private static readonly Color gridColor = new Color(0.85f, 0.85f, 0.85f, 0.7f);
    private static readonly Color occupiedCellColor = new Color(1f, 1f, 1f, 0.95f);
    private static readonly Color radiusColor = new Color(0.6f, 1f, 0.2f, 1f);
    private static readonly Color detectedColor = new Color(1f, 0.25f, 0.25f, 1f);
    private static readonly Color candidateColor = new Color(1f, 0.9f, 0.2f, 1f);
    private static readonly Color skippedColor = new Color(0.8f, 0.4f, 1f, 1f);
    private static readonly Color outsideColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);

    private static readonly List<Enemy> detected = new List<Enemy>();
    private static readonly List<Enemy> candidates = new List<Enemy>();
    private static readonly List<Enemy> skipped = new List<Enemy>();
    private static readonly Vector3[] rectBuffer = new Vector3[5];
    private static readonly Vector3[] circleBuffer = new Vector3[33];
    private static GUIStyle labelStyle;
    private static GUIStyle panelStyle;

    private const string PrefEnabled = "EnemySpatialGridGizmo.Enabled";
    private const string PrefStats = "EnemySpatialGridGizmo.Stats";
    private const string PrefLabels = "EnemySpatialGridGizmo.Labels";
    private const string PrefDistance = "EnemySpatialGridGizmo.Distance";
    private const string PrefDefaultGizmos = "EnemySpatialGridGizmo.EnemyDefaultGizmos";

    private const string MenuEnabled = "Tools/Enemy Spatial Grid Gizmo/Show Spatial Grid";
    private const string MenuStats = "Tools/Enemy Spatial Grid Gizmo/Show Stats Panel";
    private const string MenuLabels = "Tools/Enemy Spatial Grid Gizmo/Show Cell Labels";
    private const string MenuDistance = "Tools/Enemy Spatial Grid Gizmo/Show Distance Labels";
    private const string MenuDefaultGizmos = "Tools/Enemy Spatial Grid Gizmo/Show Enemy Default Gizmos (Block, Contact)";

    private static bool gizmoEnabled = true;

    static EnemySpatialGridGizmo()
    {
        gizmoEnabled = EditorPrefs.GetBool(PrefEnabled, true);
        showStatsPanel = EditorPrefs.GetBool(PrefStats, true);
        showCellLabels = EditorPrefs.GetBool(PrefLabels, true);
        showDistanceLabels = EditorPrefs.GetBool(PrefDistance, true);
        Enemy.DrawDefaultGizmos = EditorPrefs.GetBool(PrefDefaultGizmos, true);

        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    [MenuItem(MenuEnabled)]
    private static void ToggleEnabled() => SetPref(PrefEnabled, ref gizmoEnabled, !gizmoEnabled);
    [MenuItem(MenuEnabled, true)]
    private static bool ToggleEnabledValidate() { Menu.SetChecked(MenuEnabled, gizmoEnabled); return true; }

    [MenuItem(MenuStats)]
    private static void ToggleStats() => SetPref(PrefStats, ref showStatsPanel, !showStatsPanel);
    [MenuItem(MenuStats, true)]
    private static bool ToggleStatsValidate() { Menu.SetChecked(MenuStats, showStatsPanel); return true; }

    [MenuItem(MenuLabels)]
    private static void ToggleLabels() => SetPref(PrefLabels, ref showCellLabels, !showCellLabels);
    [MenuItem(MenuLabels, true)]
    private static bool ToggleLabelsValidate() { Menu.SetChecked(MenuLabels, showCellLabels); return true; }

    [MenuItem(MenuDistance)]
    private static void ToggleDistance() => SetPref(PrefDistance, ref showDistanceLabels, !showDistanceLabels);
    [MenuItem(MenuDistance, true)]
    private static bool ToggleDistanceValidate() { Menu.SetChecked(MenuDistance, showDistanceLabels); return true; }

    // Enemy.OnDrawGizmosSelected (빨간 원기둥 + 노란 contactRange) 표시 여부
    [MenuItem(MenuDefaultGizmos)]
    private static void ToggleDefaultGizmos()
    {
        Enemy.DrawDefaultGizmos = !Enemy.DrawDefaultGizmos;
        EditorPrefs.SetBool(PrefDefaultGizmos, Enemy.DrawDefaultGizmos);
        SceneView.RepaintAll();
    }
    [MenuItem(MenuDefaultGizmos, true)]
    private static bool ToggleDefaultGizmosValidate() { Menu.SetChecked(MenuDefaultGizmos, Enemy.DrawDefaultGizmos); return true; }

    private static void SetPref(string key, ref bool field, bool value)
    {
        field = value;
        EditorPrefs.SetBool(key, value);
        SceneView.RepaintAll();
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!gizmoEnabled || !Application.isPlaying) return;

        EnemyManager manager = EnemyManager.Instance;
        if (manager == null) return;

        // 자식 메시가 선택돼도 부모의 Enemy를 찾도록 합니다.
        GameObject selectedObj = Selection.activeGameObject;
        if (selectedObj == null) return;
        Enemy selected = selectedObj.GetComponentInParent<Enemy>();
        if (selected == null || !selected.gameObject.activeInHierarchy || !IsManagedEnemy(manager, selected))
            return;

        Draw(manager, selected);
        sceneView.Repaint();
    }

    // 보스처럼 그리드에 등록되지 않는 Enemy는 제외합니다.
    private static bool IsManagedEnemy(EnemyManager manager, Enemy enemy)
    {
        IReadOnlyList<Enemy> enemies = manager.ActiveEnemies;
        for (int i = 0; i < enemies.Count; i++)
            if (enemies[i] == enemy) return true;
        return false;
    }

    private static void Draw(EnemyManager manager, Enemy selected)
    {
        Vector3 selPos = selected.transform.position;
        float y = selPos.y + groundOffset;
        float cellSize = manager.cellSize;
        Vector2Int selectedCell = manager.GetCellPos(selPos);
        IReadOnlyDictionary<Vector2Int, List<Enemy>> grid = manager.SpatialGrid;

        // 1. 격자 (탐색하지 않는 영역까지 포함해 공간분할 자체를 보여줌)
        int r = gridDrawRange;
        float minX = (selectedCell.x - r) * cellSize;
        float maxX = (selectedCell.x + r + 1) * cellSize;
        float minZ = (selectedCell.y - r) * cellSize;
        float maxZ = (selectedCell.y + r + 1) * cellSize;
        Handles.color = gridColor;
        for (int i = -r; i <= r + 1; i++)
        {
            float x = (selectedCell.x + i) * cellSize;
            float z = (selectedCell.y + i) * cellSize;
            Handles.DrawAAPolyLine(thinLineWidth, new Vector3(x, y, minZ), new Vector3(x, y, maxZ));
            Handles.DrawAAPolyLine(thinLineWidth, new Vector3(minX, y, z), new Vector3(maxX, y, z));
        }

        // 2. 점유된 셀 / 3x3 바깥 Enemy
        int occupiedCellCount = 0;
        foreach (KeyValuePair<Vector2Int, List<Enemy>> kv in grid)
        {
            if (kv.Value.Count == 0) continue;
            occupiedCellCount++;

            int dx = kv.Key.x - selectedCell.x;
            int dz = kv.Key.y - selectedCell.y;
            if (Mathf.Abs(dx) > r || Mathf.Abs(dz) > r) continue;

            bool near = Mathf.Abs(dx) <= 1 && Mathf.Abs(dz) <= 1;
            if (showCellLabels)
            {
                Vector3 center = new Vector3((kv.Key.x + 0.5f) * cellSize, y, (kv.Key.y + 0.5f) * cellSize);
                DrawLabel(center, $"({kv.Key.x},{kv.Key.y}) x{kv.Value.Count}", near ? centerCellColor : gridColor);
            }
            if (near) continue;

            DrawCellRect(kv.Key, cellSize, y, occupiedCellColor, thinLineWidth);
            for (int i = 0; i < kv.Value.Count; i++)
            {
                Enemy e = kv.Value[i];
                if (e == null) continue;
                DrawCircle(e.transform.position + Vector3.up * groundOffset, markerRadius * 0.6f, outsideColor, thinLineWidth);
            }
        }

        // 3. 3x3 이웃 셀 + GetSeparationForce와 동일한 순서/조건으로 분류
        detected.Clear();
        candidates.Clear();
        skipped.Clear();
        float radius = selected.separationRadius;
        float sqrRadius = radius * radius;
        int checkCount = 0;

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                Vector2Int cell = new Vector2Int(selectedCell.x + x, selectedCell.y + z);
                bool isCenter = x == 0 && z == 0;
                DrawCellRect(cell, cellSize, y + (isCenter ? 0.01f : 0f),
                    isCenter ? centerCellColor : neighborCellColor, isCenter ? lineWidth : thinLineWidth);

                if (!grid.TryGetValue(cell, out List<Enemy> neighbors)) continue;

                for (int i = 0; i < neighbors.Count; i++)
                {
                    Enemy n = neighbors[i];
                    if (n == null || n == selected) continue;

                    if (checkCount >= EnemyManager.SeparationMaxChecks)
                    {
                        skipped.Add(n);
                        continue;
                    }

                    Vector3 diff = selPos - n.transform.position;
                    diff.y = 0f;
                    float sqrDist = diff.sqrMagnitude;
                    if (sqrDist > 0f && sqrDist < sqrRadius)
                    {
                        detected.Add(n);
                        checkCount++;
                    }
                    else
                    {
                        candidates.Add(n);
                    }
                }
            }
        }

        // 4. 선택 Enemy, 반경, 이웃 연결선
        Vector3 selGround = selPos + Vector3.up * groundOffset;
        DrawCircle(selGround, radius, radiusColor, lineWidth);
        DrawCircle(selGround, markerRadius, centerCellColor, lineWidth);

        for (int i = 0; i < candidates.Count; i++)
        {
            Vector3 p = candidates[i].transform.position + Vector3.up * groundOffset;
            Handles.color = candidateColor;
            Handles.DrawAAPolyLine(thinLineWidth, selGround, p);
            DrawCircle(p, markerRadius * 0.7f, candidateColor, thinLineWidth);
        }

        for (int i = 0; i < skipped.Count; i++)
        {
            Vector3 p = skipped[i].transform.position + Vector3.up * groundOffset;
            DrawCircle(p, markerRadius * 0.7f, skippedColor, thinLineWidth);
        }

        for (int i = 0; i < detected.Count; i++)
        {
            Vector3 p = detected[i].transform.position + Vector3.up * groundOffset;
            Handles.color = detectedColor;
            Handles.DrawAAPolyLine(lineWidth, selGround, p);
            DrawCircle(p, markerRadius, detectedColor, lineWidth);

            Vector3 diff = p - selGround; diff.y = 0f;
            if (showDistanceLabels)
                DrawLabel(p + Vector3.up * 0.3f, $"{diff.magnitude:0.00}m", detectedColor);
        }

        // 5. 통계 패널
        if (showStatsPanel)
            DrawStatsPanel(manager, selected, selectedCell, occupiedCellCount);
    }

    private static void DrawStatsPanel(EnemyManager manager, Enemy selected, Vector2Int selectedCell, int occupiedCellCount)
    {
        EnsureStyles();
        int totalEnemies = manager.ActiveEnemies.Count;
        int checkedEnemies = detected.Count + candidates.Count + skipped.Count;
        float percent = totalEnemies > 0 ? checkedEnemies * 100f / totalEnemies : 0f;

        string text =
            "[Spatial Grid]\n" +
            $"선택: {selected.name}   셀: ({selectedCell.x}, {selectedCell.y})   cellSize: {manager.cellSize}m\n" +
            $"활성 Enemy: {totalEnemies}   점유 셀: {occupiedCellCount}   탐색 셀: 9 (3x3)\n" +
            $"탐색 후보: {checkedEnemies} / {totalEnemies}   (전체 대비 {percent:0.#}%)\n" +
            $"<color=#ff4040>감지 {detected.Count}</color>   <color=#ffe633>반경 밖 {candidates.Count}</color>   " +
            $"<color=#cc66ff>검사 생략 {skipped.Count}</color>   반경 {selected.separationRadius}m   max {EnemyManager.SeparationMaxChecks}";

        Handles.BeginGUI();
        Vector2 size = panelStyle.CalcSize(new GUIContent(text));
        GUI.Box(new Rect(10f, 10f, size.x + 20f, size.y + 16f), text, panelStyle);
        Handles.EndGUI();
    }

    private static void DrawCellRect(Vector2Int cell, float cellSize, float y, Color color, float width)
    {
        float x0 = cell.x * cellSize;
        float z0 = cell.y * cellSize;
        rectBuffer[0] = new Vector3(x0, y, z0);
        rectBuffer[1] = new Vector3(x0 + cellSize, y, z0);
        rectBuffer[2] = new Vector3(x0 + cellSize, y, z0 + cellSize);
        rectBuffer[3] = new Vector3(x0, y, z0 + cellSize);
        rectBuffer[4] = rectBuffer[0];
        Handles.color = color;
        Handles.DrawAAPolyLine(width, rectBuffer);
    }

    private static void DrawCircle(Vector3 center, float radius, Color color, float width)
    {
        int seg = circleBuffer.Length - 1;
        for (int i = 0; i <= seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            circleBuffer[i] = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
        }
        Handles.color = color;
        Handles.DrawAAPolyLine(width, circleBuffer);
    }

    private static void DrawLabel(Vector3 worldPos, string text, Color color)
    {
        EnsureStyles();
        labelStyle.normal.textColor = color;
        Handles.Label(worldPos, text, labelStyle);
    }

    private static void EnsureStyles()
    {
        if (labelStyle != null) return;

        Texture2D bg = new Texture2D(1, 1);
        bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.7f));
        bg.Apply();
        bg.hideFlags = HideFlags.HideAndDontSave;

        labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            padding = new RectOffset(4, 4, 2, 2)
        };
        labelStyle.normal.background = bg;

        panelStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.UpperLeft,
            richText = true,
            fontSize = 13,
            padding = new RectOffset(10, 10, 8, 8),
            wordWrap = false
        };
        panelStyle.normal.background = bg;
        panelStyle.normal.textColor = Color.white;
    }
}
