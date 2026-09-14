using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// [에디터 전용] Play 중 씬뷰에서 Enemy를 선택하면 다층 추적(2단 레이캐스트) 로직을 그려줍니다.
/// Enemy.FloorDebug 스냅샷(0.1초마다 갱신)을 읽기만 하며, 게임 빌드에는 포함되지 않습니다.
///
/// 표시 요소:
///   주황 선            : Low Ray (발 위 2m → 아래 4m, 같은 층 추적)
///   하늘색 선          : High Ray (플레이어 위 5m / Enemy 위 50m → 아래 100m, 여러 층 관통)
///   회색 디스크        : High Ray가 관통한 각 층의 히트 지점 (y, 플레이어와의 높이차 라벨)
///   초록 디스크        : 그중 선택된 층 (플레이어 y와 가장 가까운 히트)
///   흰 점선            : 플레이어 높이 기준선
///   자홍 점선          : +2.0m 임계선 (이보다 플레이어가 높으면 High Ray 모드)
///   수직 화살표        : Enemy → 목표 높이 (상태별 색: 스텝/등반/낙하)
///   연두 궤적          : 최근 선택된 층의 이동 궤적 (토글)
///   빨간 X             : 단일 레이(최상층 첫 히트)였다면 골랐을 층 — 비교용 (토글)
/// </summary>
[InitializeOnLoad]
public static class EnemyFloorTrackingGizmo
{
    // 설정 (필요하면 여기 값만 바꾸면 됩니다)
    private static float lineWidth = 6f;
    private static float thinLineWidth = 3f;
    private static float discRadius = 0.45f;
    private static float referenceLineHalfLength = 3f;
    private static float compactHighRayMargin = 4f;   // Compact 모드에서 최상층 히트 위로 얼마나 더 그릴지

    private static readonly Color lowRayColor = new Color(1f, 0.6f, 0.15f, 1f);
    private static readonly Color highRayColor = new Color(0.3f, 0.85f, 1f, 1f);
    private static readonly Color hitColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    private static readonly Color chosenColor = new Color(0.3f, 1f, 0.3f, 1f);
    private static readonly Color playerLineColor = new Color(1f, 1f, 1f, 0.9f);
    private static readonly Color thresholdColor = new Color(1f, 0.4f, 0.9f, 0.9f);
    private static readonly Color predictColor = new Color(1f, 1f, 0.4f, 1f);
    private static readonly Color historyColor = new Color(0.6f, 1f, 0.3f, 0.9f);
    private static readonly Color naiveColor = new Color(1f, 0.25f, 0.25f, 1f);
    private static readonly Color missColor = new Color(1f, 0.25f, 0.25f, 1f);
    private static readonly Color stateGrounded = new Color(0.7f, 0.7f, 0.7f, 1f);
    private static readonly Color stateFastStep = new Color(0.4f, 1f, 0.4f, 1f);
    private static readonly Color stateSlowClimb = new Color(1f, 0.8f, 0.2f, 1f);
    private static readonly Color stateFalling = new Color(1f, 0.35f, 0.35f, 1f);

    private const string Root = "Tools/Enemy Floor Tracking Gizmo/";
    private const string MenuEnabled = Root + "Show Floor Tracking";
    private const string MenuHitLabels = Root + "Show Hit Labels";
    private const string MenuHistory = Root + "Show History Trail";
    private const string MenuNaive = Root + "Show Naive (Single Ray) Comparison";
    private const string MenuStats = Root + "Show Stats Panel";
    private const string MenuCompact = Root + "Compact High Ray";
    private const string MenuLegend = Root + "Show Legend";

    private const string PrefPrefix = "EnemyFloorTrackingGizmo.";

    private static bool gizmoEnabled = true;
    private static bool showHitLabels = true;
    private static bool showHistory = true;
    private static bool showNaive = true;
    private static bool showStats = true;
    private static bool compactHighRay = true;
    private static bool showLegend = true;

    private static readonly RaycastHit[] naiveBuffer = new RaycastHit[10];
    private static readonly Vector3[] circleBuffer = new Vector3[33];
    private static GUIStyle labelStyle;
    private static GUIStyle panelStyle;

    static EnemyFloorTrackingGizmo()
    {
        gizmoEnabled = EditorPrefs.GetBool(PrefPrefix + "Enabled", true);
        showHitLabels = EditorPrefs.GetBool(PrefPrefix + "HitLabels", true);
        showHistory = EditorPrefs.GetBool(PrefPrefix + "History", true);
        showNaive = EditorPrefs.GetBool(PrefPrefix + "Naive", true);
        showStats = EditorPrefs.GetBool(PrefPrefix + "Stats", true);
        compactHighRay = EditorPrefs.GetBool(PrefPrefix + "Compact", true);
        showLegend = EditorPrefs.GetBool(PrefPrefix + "Legend", true);

        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    // ==========================================
    // 메뉴 토글
    // ==========================================
    [MenuItem(MenuEnabled)] private static void T1() => Toggle("Enabled", ref gizmoEnabled);
    [MenuItem(MenuEnabled, true)] private static bool V1() { Menu.SetChecked(MenuEnabled, gizmoEnabled); return true; }

    [MenuItem(MenuHitLabels)] private static void T2() => Toggle("HitLabels", ref showHitLabels);
    [MenuItem(MenuHitLabels, true)] private static bool V2() { Menu.SetChecked(MenuHitLabels, showHitLabels); return true; }

    [MenuItem(MenuHistory)] private static void T3() => Toggle("History", ref showHistory);
    [MenuItem(MenuHistory, true)] private static bool V3() { Menu.SetChecked(MenuHistory, showHistory); return true; }

    [MenuItem(MenuNaive)] private static void T4() => Toggle("Naive", ref showNaive);
    [MenuItem(MenuNaive, true)] private static bool V4() { Menu.SetChecked(MenuNaive, showNaive); return true; }

    [MenuItem(MenuStats)] private static void T5() => Toggle("Stats", ref showStats);
    [MenuItem(MenuStats, true)] private static bool V5() { Menu.SetChecked(MenuStats, showStats); return true; }

    [MenuItem(MenuCompact)] private static void T6() => Toggle("Compact", ref compactHighRay);
    [MenuItem(MenuCompact, true)] private static bool V6() { Menu.SetChecked(MenuCompact, compactHighRay); return true; }

    [MenuItem(MenuLegend)] private static void T7() => Toggle("Legend", ref showLegend);
    [MenuItem(MenuLegend, true)] private static bool V7() { Menu.SetChecked(MenuLegend, showLegend); return true; }

    private static void Toggle(string key, ref bool field)
    {
        field = !field;
        EditorPrefs.SetBool(PrefPrefix + key, field);
        SceneView.RepaintAll();
    }

    // ==========================================
    // 진입점
    // ==========================================
    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!gizmoEnabled || !Application.isPlaying) return;

        GameObject selectedObj = Selection.activeGameObject;
        if (selectedObj == null) return;
        Enemy enemy = selectedObj.GetComponentInParent<Enemy>();
        if (enemy == null || !enemy.gameObject.activeInHierarchy || !enemy.FloorDebug.valid) return;

        Draw(sceneView, enemy);
        sceneView.Repaint();
    }

    private static void Draw(SceneView sceneView, Enemy enemy)
    {
        Enemy.FloorTrackSnapshot s = enemy.FloorDebug;
        Vector3 enemyPos = enemy.transform.position;
        Vector3 checkPos = s.checkPos;

        // 1. 궤적 (가장 뒤에 깔리도록 먼저)
        if (showHistory) DrawHistory(enemy.FloorDebugHistory);

        // 2. 예측 위치 (현재 → checkPos)
        Handles.color = predictColor;
        Handles.DrawAAPolyLine(thinLineWidth, enemyPos, checkPos);
        DrawCircle(checkPos, 0.15f, predictColor, thinLineWidth);
        DrawLabel(checkPos + Vector3.up * 0.25f, "예측 위치 (+0.1s)", predictColor);

        // 3. 기준선: 플레이어 높이 / +2.0m 임계선
        DrawHorizontalDotted(checkPos, s.playerY, playerLineColor);
        DrawLabel(new Vector3(checkPos.x + referenceLineHalfLength, s.playerY, checkPos.z), $"Player Y = {s.playerY:0.0}", playerLineColor);
        float thresholdY = enemyPos.y + 2f;
        DrawHorizontalDotted(checkPos, thresholdY, thresholdColor);
        DrawLabel(new Vector3(checkPos.x - referenceLineHalfLength, thresholdY, checkPos.z), "+2.0m 임계선", thresholdColor);

        // 4. Low Ray
        bool usedLow = s.mode == Enemy.EFloorTrackMode.LowRay || s.mode == Enemy.EFloorTrackMode.LowRayMissFallback;
        if (usedLow)
        {
            Vector3 end = s.lowRayStart + Vector3.down * s.lowRayDistance;
            Handles.color = lowRayColor;
            Handles.DrawAAPolyLine(lineWidth, s.lowRayStart, end);
            DrawCircle(s.lowRayStart, 0.2f, lowRayColor, thinLineWidth);
            DrawLabel(s.lowRayStart + Vector3.up * 0.3f, $"Low Ray (↑2m → ↓{s.lowRayDistance:0}m)", lowRayColor);

            if (s.lowRayHit)
            {
                DrawDisc(s.lowHitPoint, discRadius, chosenColor, lineWidth);
                if (showHitLabels) DrawLabel(s.lowHitPoint + Vector3.up * 0.3f, $"바닥 y={s.lowHitPoint.y:0.00} (선택)", chosenColor);
            }
            else
            {
                DrawCross(end, 0.35f, missColor, lineWidth);
                DrawLabel(end + Vector3.down * 0.3f, "MISS → High Ray fallback", missColor);
            }
        }

        // 5. High Ray
        bool usedHigh = s.mode == Enemy.EFloorTrackMode.PlayerHigher || s.mode == Enemy.EFloorTrackMode.LowRayMissFallback;
        if (usedHigh)
        {
            Vector3 realStart = s.highRayStart;
            Vector3 end = realStart + Vector3.down * s.highRayDistance;
            Vector3 drawStart = realStart;

            if (compactHighRay)
            {
                float topY = enemyPos.y;
                for (int i = 0; i < s.highHitCount; i++) topY = Mathf.Max(topY, s.highHits[i].y);
                topY = Mathf.Max(topY, s.playerY);
                drawStart.y = Mathf.Min(realStart.y, topY + compactHighRayMargin);
            }

            Handles.color = highRayColor;
            Handles.DrawAAPolyLine(lineWidth, drawStart, end);
            if (compactHighRay && drawStart.y < realStart.y)
            {
                Handles.DrawDottedLine(drawStart, drawStart + Vector3.up * 1.5f, 4f);
                DrawLabel(drawStart + Vector3.up * 1.8f, $"High Ray 시작 y={realStart.y:0.0} (↓{s.highRayDistance:0}m)", highRayColor);
            }
            else
            {
                DrawCircle(realStart, 0.25f, highRayColor, thinLineWidth);
                DrawLabel(realStart + Vector3.up * 0.4f, $"High Ray 시작 (↓{s.highRayDistance:0}m)", highRayColor);
            }

            // 관통 히트들
            for (int i = 0; i < s.highHitCount; i++)
            {
                bool chosen = i == s.chosenHitIndex;
                Vector3 p = s.highHits[i];
                DrawDisc(p, chosen ? discRadius * 1.3f : discRadius, chosen ? chosenColor : hitColor, chosen ? lineWidth : thinLineWidth);
                if (showHitLabels)
                {
                    float diff = Mathf.Abs(p.y - s.playerY);
                    string text = chosen ? $"선택  y={p.y:0.00}  Δplayer={diff:0.00}" : $"y={p.y:0.00}  Δplayer={diff:0.00}";
                    DrawLabel(p + Vector3.up * 0.3f + Vector3.right * 0.6f, text, chosen ? chosenColor : hitColor);
                }
            }
        }

        // 6. 목표 높이로의 수직 화살표 + 상태
        if (s.hitGround)
        {
            Vector3 target = new Vector3(enemyPos.x, s.targetHeight, enemyPos.z);
            Color stateColor = GetStateColor(s.moveState);
            Handles.color = stateColor;
            Handles.DrawAAPolyLine(lineWidth, enemyPos, target);
            if ((target - enemyPos).sqrMagnitude > 0.0001f)
            {
                Quaternion rot = Quaternion.LookRotation(target - enemyPos);
                Handles.ConeHandleCap(0, target, rot, 0.4f, EventType.Repaint);
            }
            DrawLabel(enemyPos + Vector3.up * 1.2f + Vector3.right * 0.8f, GetStateText(enemy, s), stateColor);
        }

        // 7. 단일 레이 비교
        if (showNaive) DrawNaiveComparison(enemy, s);

        // 8. 패널
        if (showStats || showLegend) DrawPanels(sceneView, enemy, s);
    }

    // ==========================================
    // 단일 레이(최상층 첫 히트) 비교
    // ==========================================
    private static void DrawNaiveComparison(Enemy enemy, Enemy.FloorTrackSnapshot s)
    {
        Vector3 start = s.checkPos + Vector3.up * enemy.raycastStartHeight;
        int count = Physics.RaycastNonAlloc(start, Vector3.down, naiveBuffer, enemy.raycastDistance, enemy.groundLayer);
        if (count == 0) return;

        // 단일 레이의 "첫 히트" = 시작점에서 가장 가까운 = 가장 높은 층
        int top = 0;
        for (int i = 1; i < count; i++)
            if (naiveBuffer[i].distance < naiveBuffer[top].distance) top = i;

        Vector3 naivePoint = naiveBuffer[top].point;
        bool differs = !s.hitGround || Mathf.Abs(naivePoint.y - s.targetHeight) > 0.05f;

        if (differs)
        {
            DrawCross(naivePoint + Vector3.right * 0.8f, 0.4f, naiveColor, lineWidth);
            DrawLabel(naivePoint + Vector3.right * 0.8f + Vector3.up * 0.5f,
                $"단일 레이였다면 → y={naivePoint.y:0.00} (잘못된 층)", naiveColor);
        }
        else
        {
            DrawLabel(naivePoint + Vector3.right * 1.2f + Vector3.down * 0.35f, "단일 레이 결과와 동일", new Color(0.7f, 0.7f, 0.7f, 1f));
        }
    }

    // ==========================================
    // 궤적
    // ==========================================
    private static void DrawHistory(List<Vector3> history)
    {
        if (history.Count < 2) return;
        Handles.color = historyColor;
        for (int i = 1; i < history.Count; i++)
        {
            float t = i / (float)(history.Count - 1);
            Handles.color = new Color(historyColor.r, historyColor.g, historyColor.b, Mathf.Lerp(0.2f, 0.9f, t));
            Handles.DrawAAPolyLine(thinLineWidth, history[i - 1] + Vector3.up * 0.05f, history[i] + Vector3.up * 0.05f);
        }
        for (int i = 0; i < history.Count; i += 5)
            Handles.DrawSolidDisc(history[i] + Vector3.up * 0.05f, Vector3.up, 0.08f);
    }

    // ==========================================
    // 패널
    // ==========================================
    private static void DrawPanels(SceneView sceneView, Enemy enemy, Enemy.FloorTrackSnapshot s)
    {
        EnsureStyles();
        Handles.BeginGUI();
        float y = 10f;
        float right = sceneView.position.width - 10f;

        if (showStats)
        {
            string mode = s.mode switch
            {
                Enemy.EFloorTrackMode.PlayerHigher => "A. 플레이어 위층 → High Ray",
                Enemy.EFloorTrackMode.LowRay => "B. 같은 층 → Low Ray",
                Enemy.EFloorTrackMode.LowRayMissFallback => "B'. Low Ray MISS → High Ray fallback",
                _ => "-"
            };
            string text =
                "[Floor Tracking]\n" +
                $"선택: {enemy.name}\n" +
                $"모드: {mode}\n" +
                $"Enemy y={enemy.transform.position.y:0.00}   Player y={s.playerY:0.00}   차이={s.playerY - enemy.transform.position.y:+0.00;-0.00}\n" +
                (s.highHitCount > 0 ? $"High Ray 관통 층: {s.highHitCount}개 → 선택 #{s.chosenHitIndex + 1}\n" : "") +
                $"목표 높이 y={s.targetHeight:0.00}   높이차={s.heightDiff:+0.00;-0.00}\n" +
                $"상태: {GetStateText(enemy, s)}";
            Vector2 size = panelStyle.CalcSize(new GUIContent(text));
            GUI.Box(new Rect(right - size.x - 20f, y, size.x + 20f, size.y + 16f), text, panelStyle);
            y += size.y + 26f;
        }

        if (showLegend)
        {
            string legend =
                "<color=#ff9926>━</color> Low Ray (같은 층)   <color=#4dd9ff>━</color> High Ray (다층 관통)\n" +
                "<color=#cccccc>◯</color> 관통한 층   <color=#4dff4d>◉</color> 선택된 층 (Player Y와 최근접)\n" +
                "<color=#ffffff>┄</color> Player Y   <color=#ff66e6>┄</color> +2.0m 임계선   <color=#ffff66>●</color> 예측 위치\n" +
                "<color=#4dff4d>↑</color> 스텝   <color=#ffcc33>↑</color> 등반   <color=#ff5959>↓</color> 낙하   " +
                "<color=#99ff4d>—</color> 궤적   <color=#ff4040>✕</color> 단일 레이 결과";
            Vector2 size = panelStyle.CalcSize(new GUIContent(legend));
            GUI.Box(new Rect(right - size.x - 20f, y, size.x + 20f, size.y + 16f), legend, panelStyle);
        }

        Handles.EndGUI();
    }

    private static string GetStateText(Enemy enemy, Enemy.FloorTrackSnapshot s)
    {
        return s.moveState switch
        {
            Enemy.EFloorMoveState.Grounded => "접지 (목표 높이 도달)",
            Enemy.EFloorMoveState.FastStep => $"Fast Step ({enemy.fastStepSpeed:0}m/s, Δ≤0.5m)",
            Enemy.EFloorMoveState.SlowClimb => $"Slow Climb ({enemy.slowClimbSpeed:0}m/s, 수평 정지)",
            Enemy.EFloorMoveState.Falling => $"낙하 (gravity {enemy.gravity:0})",
            _ => "-"
        };
    }

    private static Color GetStateColor(Enemy.EFloorMoveState state)
    {
        return state switch
        {
            Enemy.EFloorMoveState.FastStep => stateFastStep,
            Enemy.EFloorMoveState.SlowClimb => stateSlowClimb,
            Enemy.EFloorMoveState.Falling => stateFalling,
            _ => stateGrounded
        };
    }

    // ==========================================
    // 그리기 유틸
    // ==========================================
    private static void DrawHorizontalDotted(Vector3 center, float y, Color color)
    {
        Handles.color = color;
        Vector3 a = new Vector3(center.x - referenceLineHalfLength, y, center.z);
        Vector3 b = new Vector3(center.x + referenceLineHalfLength, y, center.z);
        Handles.DrawDottedLine(a, b, 4f);
        Vector3 c = new Vector3(center.x, y, center.z - referenceLineHalfLength);
        Vector3 d = new Vector3(center.x, y, center.z + referenceLineHalfLength);
        Handles.DrawDottedLine(c, d, 4f);
    }

    private static void DrawDisc(Vector3 center, float radius, Color color, float width)
    {
        Handles.color = new Color(color.r, color.g, color.b, 0.25f);
        Handles.DrawSolidDisc(center, Vector3.up, radius);
        DrawCircle(center, radius, color, width);
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

    private static void DrawCross(Vector3 center, float size, Color color, float width)
    {
        Handles.color = color;
        Handles.DrawAAPolyLine(width, center + new Vector3(-size, 0f, -size), center + new Vector3(size, 0f, size));
        Handles.DrawAAPolyLine(width, center + new Vector3(-size, 0f, size), center + new Vector3(size, 0f, -size));
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
            alignment = TextAnchor.MiddleLeft,
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
