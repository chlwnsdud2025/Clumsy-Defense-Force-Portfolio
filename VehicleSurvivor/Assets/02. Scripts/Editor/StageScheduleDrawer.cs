using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 페이즈 구성 막대 · 웨이브 스케줄 타임라인/표를 그리는 공용 드로어.
/// StageScheduleDebugViewEditor(인스펙터)와 StageScheduleCaptureWindow(캡처 창)가 함께 사용한다.
/// </summary>
public static class StageScheduleDrawer
{
    // ==========================================================
    // 팔레트
    // ==========================================================
    public struct Palette
    {
        public Color bg, panel, panelAlt, line, text, dim, current, final, normal, nightmare, nightmareBg, boss, playhead;

        public static Palette Dark => new Palette
        {
            bg = Hex(0x383838), panel = Hex(0x4A4A4A), panelAlt = Hex(0x3D3D3D), line = Hex(0x1E1E1E),
            text = Hex(0xE8E8E8), dim = Hex(0xA0A0A0), current = Hex(0x365A86), final = Hex(0x3A3F4A),
            normal = Hex(0x8FCF8F), nightmare = Hex(0xF26A6A), nightmareBg = Hex(0x4A2E2E), boss = Hex(0xF5C542), playhead = Color.white,
        };

        public static Palette Light => new Palette
        {
            bg = Color.white, panel = Hex(0xF4F4F5), panelAlt = Hex(0xFAFAFA), line = Hex(0x8A8F99),
            text = Hex(0x1A1D24), dim = Hex(0x6B7280), current = Hex(0xDCE8F8), final = Hex(0xE9EAEE),
            normal = Hex(0x2F8F4E), nightmare = Hex(0xD9463F), nightmareBg = Hex(0xFBE9E7), boss = Hex(0xB8860B), playhead = Hex(0x1A1D24),
        };
    }

    private static readonly Color[] PoolPalette =
    {
        Hex(0x4CAF50), Hex(0x29B6F6), Hex(0xFFCA28), Hex(0xAB47BC), Hex(0xFF7043), Hex(0x26A69A),
        Hex(0xEC407A), Hex(0x8D6E63), Hex(0x9CCC65), Hex(0x5C6BC0), Hex(0xFFA726), Hex(0x78909C),
    };

    // ==========================================================
    // 그리기 컨텍스트
    // ==========================================================
    public class Context
    {
        public StageTheme_SO theme;
        public StageRunManager manager;
        public Palette palette = Palette.Dark;
        public float scale = 1f;
        public bool captureMode;
        public bool showEnemyNames = true;

        // 플레이 모드 정보
        public bool playing;
        public int currentPhase = -1;
        public float phaseElapsed;
        public bool isNightmare;
        public float nightmareElapsed;

        // 테마에서 실제 사용되는 풀 목록 (색상 인덱스용). RefreshRuntime 때 갱신.
        public List<EnemyPoolDataSO> pools = new List<EnemyPoolDataSO>();

        /// <summary>씬의 StageRunManager / RunManager에서 플레이 상태를 읽어 온다.</summary>
        public void RefreshRuntime()
        {
            pools = theme != null ? theme.CollectEnemyPools() : new List<EnemyPoolDataSO>();
            playing = Application.isPlaying && manager != null;
            currentPhase = playing ? manager.currentPhaseIndex : -1;
            phaseElapsed = 0f;
            isNightmare = false;
            nightmareElapsed = 0f;

            if (!playing || !RunManager.TryGetInstance(out RunManager runManager) || runManager.WaveTimer == null)
                return;

            WavePhaseTimer timer = runManager.WaveTimer;
            phaseElapsed = timer.PhaseElapsed;
            isNightmare = timer.IsNightmare;
            nightmareElapsed = timer.NightmareElapsed;
        }
    }

    // ==========================================================
    // 스타일 (팔레트·스케일이 바뀔 때만 재생성)
    // ==========================================================
    private class Styles
    {
        public GUIStyle title, section, label, bold, dim, small, smallBold, colHeader;
        public float rowH, barH, bossH, timelineH;
    }

    private static Styles styles;
    private static Color styleKeyColor;
    private static float styleKeyScale = -1f;

    private static Styles S(Context c)
    {
        if (styles != null && styleKeyColor == c.palette.text && Mathf.Approximately(styleKeyScale, c.scale))
            return styles;

        float s = c.scale;
        styleKeyColor = c.palette.text;
        styleKeyScale = s;

        GUIStyle Make(GUIStyle baseStyle, int size, Color color, bool bold = false, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            GUIStyle st = new GUIStyle(baseStyle)
            {
                fontSize = Mathf.RoundToInt(size * s),
                alignment = anchor,
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                richText = false,
                clipping = TextClipping.Clip,
            };
            st.normal.textColor = color;
            st.hover.textColor = color;
            return st;
        }

        styles = new Styles
        {
            title = Make(EditorStyles.label, 15, c.palette.text, true),
            section = Make(EditorStyles.label, 13, c.palette.text, true),
            label = Make(EditorStyles.label, 11, c.palette.text),
            bold = Make(EditorStyles.label, 11, c.palette.text, true),
            dim = Make(EditorStyles.label, 11, c.palette.dim),
            small = Make(EditorStyles.label, 10, c.palette.dim),
            smallBold = Make(EditorStyles.label, 10, c.palette.text, true),
            colHeader = Make(EditorStyles.label, 10, c.palette.dim, true),
            rowH = 20f * s,
            barH = 44f * s,
            bossH = 18f * s,
            timelineH = 44f * s,
        };
        return styles;
    }

    // ==========================================================
    // 섹션 1 · 페이즈 구성
    // ==========================================================
    public static void DrawPhaseSection(Context c)
    {
        Styles st = S(c);
        StageTheme_SO theme = c.theme;
        List<StagePhase_SO> phases = theme != null ? theme.phases : null;

        if (phases == null || phases.Count == 0)
        {
            EditorGUILayout.HelpBox("StageTheme_SO 또는 phases가 비어 있습니다.", MessageType.Info);
            return;
        }

        float total = 0f;
        foreach (StagePhase_SO p in phases)
            if (p != null) total += p.timeLimit;

        int midBossCount = c.manager != null && c.manager.midBossPrefabs != null ? c.manager.midBossPrefabs.Length : 0;
        string themeName = theme != null && !string.IsNullOrEmpty(theme.themeName) ? theme.themeName : (theme != null ? theme.name : "-");

        DrawSectionHeader(c, "페이즈 구성",
            $"{themeName}  ·  {phases.Count} phase  ·  총 {FormatTime(total)}  ·  중간보스 {Mathf.Min(midBossCount, phases.Count - 1)} + 최종 1");

        Rect area = GUILayoutUtility.GetRect(10f, st.barH + st.bossH + 6f * c.scale, GUILayout.ExpandWidth(true));
        Rect bar = new Rect(area.x, area.y, area.width, st.barH);
        EditorGUI.DrawRect(bar, c.palette.panel);

        float x = bar.x;
        int last = phases.Count - 1;
        for (int i = 0; i < phases.Count; i++)
        {
            StagePhase_SO p = phases[i];
            float t = p != null ? p.timeLimit : 0f;
            float w = total > 0f ? bar.width * (t / total) : bar.width / phases.Count;
            bool isFinal = i == last && phases.Count > 1;
            bool isCurrent = c.playing && i == c.currentPhase;

            Rect seg = new Rect(x, bar.y, w, bar.height);
            if (isCurrent) EditorGUI.DrawRect(seg, c.palette.current);
            else if (isFinal) EditorGUI.DrawRect(seg, c.palette.final);

            // 라벨: 위 P{n}, 아래 시간
            string head = isFinal ? $"P{i + 1} · Final" : $"P{i + 1}";
            string sub = FormatTime(t);
            if (isFinal && (p == null || p.phaseWaves == null || p.phaseWaves.Count == 0))
                sub += "  ·  정상 웨이브 없음";

            GUIStyle headStyle = new GUIStyle(st.bold) { alignment = TextAnchor.MiddleCenter };
            GUIStyle subStyle = new GUIStyle(st.small) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(seg.x, seg.y + 4f * c.scale, seg.width, seg.height * 0.5f), head, headStyle);
            GUI.Label(new Rect(seg.x, seg.y + seg.height * 0.5f - 2f * c.scale, seg.width, seg.height * 0.5f), sub, subStyle);

            if (i < last)
                EditorGUI.DrawRect(new Rect(seg.xMax - 1f, seg.y, 1f, seg.height), c.palette.line);

            // 보스 행
            Rect bossRow = new Rect(seg.x, bar.yMax + 3f * c.scale, seg.width, st.bossH);
            if (!isFinal)
            {
                string bossName = MidBossName(c.manager, i);
                if (!string.IsNullOrEmpty(bossName))
                {
                    if (i == last - 1) bossName += "  ⟶ 포탈";
                    GUIStyle bs = new GUIStyle(st.smallBold) { alignment = TextAnchor.MiddleRight };
                    bs.normal.textColor = c.palette.boss;
                    GUI.Label(new Rect(bossRow.x, bossRow.y, bossRow.width - 2f, bossRow.height), "▲ " + bossName, bs);
                }
            }
            else
            {
                string finalName = c.manager != null && c.manager.finalBossPrefab != null ? ShortBossName(c.manager.finalBossPrefab.name) : "FinalBoss";
                GUIStyle fs = new GUIStyle(st.smallBold) { alignment = TextAnchor.MiddleCenter };
                fs.normal.textColor = c.palette.nightmare;
                GUI.Label(bossRow, "◆ " + finalName + " (포탈 진입 후 소환)", fs);
            }

            x += w;
        }

        DrawFrame(bar, c.palette.line);

        if (!c.captureMode)
        {
            string note = "▲ 열쇠 획득 후 소환되는 중간보스 (씬 midBossPrefabs 순서)";
            if (c.playing) note += "  ·  강조 = 현재 페이즈";
            GUILayout.Label(note, st.small);
        }
    }

    // ==========================================================
    // 섹션 2 · 웨이브 스케줄
    // ==========================================================
    public static void DrawWaveSection(Context c, ref int selectedPhase, bool showTabs)
    {
        Styles st = S(c);
        StageTheme_SO theme = c.theme;
        List<StagePhase_SO> phases = theme != null ? theme.phases : null;

        if (phases == null || phases.Count == 0)
            return;

        selectedPhase = Mathf.Clamp(selectedPhase, 0, phases.Count - 1);
        StagePhase_SO phase = phases[selectedPhase];

        int normalCount = CountValid(phase?.phaseWaves);
        int nightmareCount = CountValid(phase?.nightmareWaves);
        DrawSectionHeader(c, "웨이브 스케줄",
            $"Phase {selectedPhase + 1}  ·  제한시간 {FormatTime(phase != null ? phase.timeLimit : 0f)}  ·  정상 {normalCount} + 악몽 {nightmareCount}");

        if (showTabs)
        {
            string[] labels = new string[phases.Count];
            for (int i = 0; i < phases.Count; i++)
                labels[i] = phases[i] != null ? $"P{i + 1}  {FormatTime(phases[i].timeLimit)}" : $"P{i + 1}";
            selectedPhase = GUILayout.Toolbar(selectedPhase, labels, GUILayout.Height(st.rowH));
            GUILayout.Space(4f * c.scale);
        }

        if (phase == null)
            return;

        DrawTimeline(c, phase, selectedPhase);
        GUILayout.Space(6f * c.scale);
        DrawTable(c, phase);
    }

    // ---------- 타임라인 ----------
    private static readonly List<WaveEnemy_DataSO> sorted = new List<WaveEnemy_DataSO>();

    private static void DrawTimeline(Context c, StagePhase_SO phase, int phaseIndex)
    {
        Styles st = S(c);
        float labelH = 15f * c.scale;
        Rect area = GUILayoutUtility.GetRect(10f, labelH + st.timelineH + labelH + 6f * c.scale, GUILayout.ExpandWidth(true));
        Rect tl = new Rect(area.x, area.y + labelH + 2f * c.scale, area.width, st.timelineH);

        // 웨이브 목록 (시작 시각 순)
        Sort(phase.phaseWaves);
        List<WaveEnemy_DataSO> normal = new List<WaveEnemy_DataSO>(sorted);
        Sort(phase.nightmareWaves);
        List<WaveEnemy_DataSO> night = new List<WaveEnemy_DataSO>(sorted);

        // 칸 폭은 균등: 정상·악몽 웨이브 수 비율로 구간을 나눈다 (웨이브가 없으면 최소 1칸)
        int nCount = Mathf.Max(normal.Count, 1);
        int mCount = Mathf.Max(night.Count, 1);
        float cellW = tl.width / (nCount + mCount);
        Rect normalRect = new Rect(tl.x, tl.y, cellW * nCount, tl.height);
        Rect nightRect = new Rect(normalRect.xMax, tl.y, cellW * mCount, tl.height);

        EditorGUI.DrawRect(normalRect, c.palette.panel);
        EditorGUI.DrawRect(nightRect, c.palette.nightmareBg);

        // 구간 제목 (막대 위)
        GUIStyle titleN = new GUIStyle(st.smallBold) { alignment = TextAnchor.MiddleCenter };
        GUIStyle titleM = new GUIStyle(st.smallBold) { alignment = TextAnchor.MiddleCenter };
        titleM.normal.textColor = c.palette.nightmare;
        GUI.Label(new Rect(normalRect.x, area.y, normalRect.width, labelH), "정상 웨이브 (phaseWaves)", titleN);
        GUI.Label(new Rect(nightRect.x, area.y, nightRect.width, labelH), "Nightmare (시간 초과 · nightmareWaves)", titleM);

        // 칸 그리기
        DrawWaveCells(c, normal, normalRect, cellW, "W", "", c.palette.normal, c.palette.line);
        DrawWaveCells(c, night, nightRect, cellW, "N", "+", c.palette.nightmare, c.palette.nightmare);

        // timeLimit 경계
        EditorGUI.DrawRect(new Rect(nightRect.x - 1f, tl.y - 3f * c.scale, 2f, tl.height + 6f * c.scale), c.palette.nightmare);

        // 축 라벨 (막대 아래): 0:00 · timeLimit · ∞
        float axisY = tl.yMax + 2f * c.scale;
        GUI.Label(new Rect(tl.x, axisY, 80f * c.scale, labelH), "0:00", st.small);
        GUIStyle limitStyle = new GUIStyle(st.smallBold) { alignment = TextAnchor.MiddleRight };
        limitStyle.normal.textColor = c.palette.nightmare;
        GUI.Label(new Rect(nightRect.x - 160f * c.scale - 4f, axisY, 160f * c.scale, labelH), $"timeLimit {FormatTime(phase.timeLimit)}", limitStyle);
        GUIStyle inf = new GUIStyle(st.small) { alignment = TextAnchor.MiddleRight };
        GUI.Label(new Rect(tl.xMax - 60f * c.scale, axisY, 58f * c.scale, labelH), "∞", inf);

        // 플레이헤드: 현재 웨이브 칸 안에서 진행 비율만큼 이동
        if (c.playing && c.currentPhase == phaseIndex)
        {
            float px = c.isNightmare
                ? PlayheadX(night, nightRect, cellW, c.nightmareElapsed, -1f)
                : PlayheadX(normal, normalRect, cellW, c.phaseElapsed, phase.timeLimit);
            EditorGUI.DrawRect(new Rect(px - 1.5f, tl.y - 4f * c.scale, 3f, tl.height + 8f * c.scale), c.palette.playhead);
            string now = c.isNightmare ? $"▶ 악몽 +{FormatTime(c.nightmareElapsed)}" : $"▶ {FormatTime(c.phaseElapsed)}";
            GUIStyle ps = new GUIStyle(st.smallBold) { alignment = TextAnchor.MiddleCenter };
            ps.normal.textColor = c.palette.bg;
            float bw = ps.CalcSize(new GUIContent(now)).x + 10f;
            Rect badge = new Rect(px - bw * 0.5f, tl.yMax - labelH - 2f * c.scale, bw, labelH);
            EditorGUI.DrawRect(badge, c.palette.playhead);
            GUI.Label(badge, now, ps);
        }

        DrawFrame(tl, c.palette.line);
    }

    // 한 구간의 웨이브 칸: 왼쪽 위 태그(W1 / ★W3), 그 아래 시작 시각
    private static void DrawWaveCells(Context c, List<WaveEnemy_DataSO> waves, Rect section, float cellW, string prefix, string timePrefix, Color tagColor, Color divider)
    {
        Styles st = S(c);
        if (waves.Count == 0)
        {
            GUIStyle none = new GUIStyle(st.small) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(section, "없음", none);
            return;
        }

        GUIStyle tag = new GUIStyle(st.bold);
        tag.normal.textColor = tagColor;
        float padX = 6f * c.scale, padY = 4f * c.scale;

        for (int i = 0; i < waves.Count; i++)
        {
            WaveEnemy_DataSO w = waves[i];
            Rect cell = new Rect(section.x + cellW * i, section.y, cellW, section.height);
            if (i > 0)
                EditorGUI.DrawRect(new Rect(cell.x, cell.y, 1f, cell.height), divider);

            string label = (HasBoss(w) ? "★ " : "") + prefix + (i + 1);
            GUI.Label(new Rect(cell.x + padX, cell.y + padY, cell.width - padX * 2f, cell.height * 0.5f - padY), label, tag);
            GUI.Label(new Rect(cell.x + padX, cell.y + cell.height * 0.5f, cell.width - padX * 2f, cell.height * 0.5f - padY), timePrefix + FormatTime(w.waveStartTime), st.small);
        }
    }

    // 경과 시간이 속한 칸을 찾아, 칸 안에서 다음 웨이브 시작까지의 진행 비율로 x를 구한다
    private static float PlayheadX(List<WaveEnemy_DataSO> waves, Rect section, float cellW, float elapsed, float endSec)
    {
        if (waves.Count == 0)
            return endSec > 0f ? section.x + section.width * Mathf.Clamp01(elapsed / endSec) : section.x;

        int idx = 0;
        for (int i = 0; i < waves.Count; i++)
            if (waves[i].waveStartTime <= elapsed) idx = i;

        float start = waves[idx].waveStartTime;
        float end = idx + 1 < waves.Count ? waves[idx + 1].waveStartTime : (endSec > 0f ? endSec : start + 60f);
        float t = end > start ? Mathf.Clamp01((elapsed - start) / (end - start)) : 0f;
        return section.x + cellW * idx + cellW * t;
    }

    // ---------- 표 ----------
    private static void DrawTable(Context c, StagePhase_SO phase)
    {
        Styles st = S(c);
        float s = c.scale;
        float colTag = 34f * s, colStart = 52f * s, colDur = 58f * s, colInterval = 50f * s, colCount = 54f * s, colRate = 56f * s, colBoss = 34f * s;

        Rect h = GUILayoutUtility.GetRect(10f, st.rowH, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(h, c.palette.panelAlt);
        float x = h.x + 4f;
        Col(ref x, h, colTag, "#", st.colHeader);
        Col(ref x, h, colStart, "시작", st.colHeader);
        Col(ref x, h, colDur, "지속", st.colHeader);
        Col(ref x, h, colInterval, "간격", st.colHeader);
        Col(ref x, h, colCount, "마리", st.colHeader);
        Col(ref x, h, colRate, "마리/초", st.colHeader);
        Col(ref x, h, colBoss, "보스", st.colHeader);
        if (c.showEnemyNames) GUI.Label(new Rect(x, h.y, h.xMax - x, h.height), "적 구성", st.colHeader);
        EditorGUI.DrawRect(new Rect(h.x, h.yMax - 1f, h.width, 1f), c.palette.line);

        int row = 0;
        DrawRows(c, phase.phaseWaves, "W", false, phase.timeLimit, ref row, colTag, colStart, colDur, colInterval, colCount, colRate, colBoss);
        DrawRows(c, phase.nightmareWaves, "N", true, -1f, ref row, colTag, colStart, colDur, colInterval, colCount, colRate, colBoss);
    }

    private static void DrawRows(Context c, List<WaveEnemy_DataSO> waves, string prefix, bool nightmare, float spanSec, ref int row,
        float colTag, float colStart, float colDur, float colInterval, float colCount, float colRate, float colBoss)
    {
        Styles st = S(c);
        Sort(waves);
        if (sorted.Count == 0)
            return;

        for (int i = 0; i < sorted.Count; i++)
        {
            WaveEnemy_DataSO w = sorted[i];
            bool last = i + 1 == sorted.Count;
            float end = last ? spanSec : sorted[i + 1].waveStartTime;

            Rect r = GUILayoutUtility.GetRect(10f, st.rowH, GUILayout.ExpandWidth(true));
            if (row++ % 2 == 1) EditorGUI.DrawRect(r, c.palette.panelAlt);

            float x = r.x + 4f;
            GUIStyle tag = new GUIStyle(st.bold); tag.normal.textColor = nightmare ? c.palette.nightmare : c.palette.normal;
            Col(ref x, r, colTag, prefix + (i + 1), tag);
            Col(ref x, r, colStart, (nightmare ? "+" : "") + FormatTime(w.waveStartTime), st.label);
            Col(ref x, r, colDur, end < 0f ? "∞" : FormatDuration(end - w.waveStartTime), st.dim);
            Col(ref x, r, colInterval, $"{w.spawnInterval:0.##}s", st.label);
            Col(ref x, r, colCount, $"{w.minSpawnCount}~{w.maxSpawnCount}", st.label);
            Col(ref x, r, colRate, $"{SpawnRate(w):0.##}", st.bold);

            GUIStyle boss = new GUIStyle(st.bold); boss.normal.textColor = c.palette.boss;
            Col(ref x, r, colBoss, HasBoss(w) ? (w.miniBossCount > 1 ? $"★{w.miniBossCount}" : "★") : "", boss);

            if (c.showEnemyNames)
                DrawEnemies(c, w, new Rect(x, r.y, r.xMax - x, r.height));

            if (!c.captureMode)
            {
                GUI.Label(r, new GUIContent("", w.name));
                Event e = Event.current;
                if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
                {
                    if (e.control || e.command) Selection.activeObject = w;
                    else EditorGUIUtility.PingObject(w);
                    e.Use();
                }
            }
        }
    }

    private static void DrawEnemies(Context c, WaveEnemy_DataSO w, Rect area)
    {
        Styles st = S(c);
        if (w.activeEnemies == null || w.activeEnemies.Count == 0) return;

        bool uniform = true;
        float first = -1f;
        foreach (WaveEnemy_Data e in w.activeEnemies)
        {
            if (e == null || e.targetPool == null) continue;
            if (first < 0f) first = e.spawnWeight;
            else if (!Mathf.Approximately(first, e.spawnWeight)) { uniform = false; break; }
        }

        float x = area.x;
        float dot = 6f * c.scale;
        foreach (WaveEnemy_Data e in w.activeEnemies)
        {
            if (e == null || e.targetPool == null) continue;
            string text = ShortPoolName(e.targetPool.name);
            if (!uniform) text += $" ×{e.spawnWeight:0.#}";
            if (e.spawnAsPurePack) text += " (팩)";

            float tw = st.label.CalcSize(new GUIContent(text)).x;
            if (x + dot + 4f + tw > area.xMax) { GUI.Label(new Rect(x, area.y, 20f, area.height), "…", st.dim); break; }

            EditorGUI.DrawRect(new Rect(x, area.y + (area.height - dot) * 0.5f, dot, dot), PoolColor(c.pools, e.targetPool));
            GUI.Label(new Rect(x + dot + 3f, area.y, tw + 2f, area.height), text, st.label);
            x += dot + 3f + tw + 10f * c.scale;
        }
    }

    // ==========================================================
    // 공통 유틸
    // ==========================================================
    private static void DrawSectionHeader(Context c, string title, string summary)
    {
        Styles st = S(c);
        Rect r = GUILayoutUtility.GetRect(10f, st.rowH + 4f * c.scale, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(new Rect(r.x, r.y + 5f * c.scale, 3f, r.height - 10f * c.scale), c.palette.normal);
        GUI.Label(new Rect(r.x + 9f, r.y, 200f * c.scale, r.height), title, st.section);
        GUIStyle right = new GUIStyle(st.dim) { alignment = TextAnchor.MiddleRight };
        GUI.Label(new Rect(r.x + 200f * c.scale, r.y, r.width - 200f * c.scale, r.height), summary, right);
        GUILayout.Space(2f * c.scale);
    }

    private static void DrawFrame(Rect r, Color color)
    {
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1f), color);
        EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1f, r.width, 1f), color);
        EditorGUI.DrawRect(new Rect(r.x, r.y, 1f, r.height), color);
        EditorGUI.DrawRect(new Rect(r.xMax - 1f, r.y, 1f, r.height), color);
    }

    private static void Col(ref float x, Rect row, float w, string text, GUIStyle style)
    {
        GUI.Label(new Rect(x, row.y, w, row.height), text, style);
        x += w;
    }

    private static void Sort(List<WaveEnemy_DataSO> waves)
    {
        sorted.Clear();
        if (waves != null)
            foreach (WaveEnemy_DataSO w in waves)
                if (w != null) sorted.Add(w);
        sorted.Sort((a, b) => a.waveStartTime.CompareTo(b.waveStartTime));
    }

    private static int CountValid(List<WaveEnemy_DataSO> waves)
    {
        int n = 0;
        if (waves != null)
            foreach (WaveEnemy_DataSO w in waves)
                if (w != null) n++;
        return n;
    }

    private static bool HasBoss(WaveEnemy_DataSO w) =>
        w.miniBossPrefabs != null && w.miniBossPrefabs.Count > 0 && w.miniBossCount > 0;

    private static float SpawnRate(WaveEnemy_DataSO w) =>
        w == null || w.spawnInterval <= 0f ? 0f : (w.minSpawnCount + w.maxSpawnCount) * 0.5f / w.spawnInterval;

    private static string MidBossName(StageRunManager manager, int phaseIndex)
    {
        if (manager == null || manager.midBossPrefabs == null || phaseIndex >= manager.midBossPrefabs.Length)
            return null;
        GameObject prefab = manager.midBossPrefabs[phaseIndex];
        return prefab != null ? ShortBossName(prefab.name) : null;
    }

    // "Bee_Boss" → "Bee", "Final_Boss" → "Final", "Planta_Boss" → "Planta"
    private static string ShortBossName(string name)
    {
        if (name.EndsWith("_Boss")) name = name.Substring(0, name.Length - 5);
        else if (name.EndsWith("Boss")) name = name.Substring(0, name.Length - 4);
        return name.Replace('_', ' ');
    }

    // "Base_Rabby_King" → "Rabby King", "Time_Over_Fly_1" → "Fly 1"
    private static string ShortPoolName(string name)
    {
        if (name.StartsWith("Base_")) name = name.Substring(5);
        else if (name.StartsWith("Time_Over_")) name = name.Substring(10);
        return name.Replace('_', ' ');
    }

    private static Color PoolColor(List<EnemyPoolDataSO> pools, EnemyPoolDataSO pool)
    {
        if (pools != null)
        {
            int idx = pools.IndexOf(pool);
            if (idx >= 0) return PoolPalette[idx % PoolPalette.Length];
        }
        return Hex(0x9E9E9E);
    }

    public static string FormatTime(float sec)
    {
        int s = Mathf.RoundToInt(sec);
        return $"{s / 60}:{s % 60:00}";
    }

    private static string FormatDuration(float sec)
    {
        int s = Mathf.RoundToInt(sec);
        if (s < 60) return $"{s}초";
        return s % 60 == 0 ? $"{s / 60}분" : $"{s / 60}분 {s % 60}초";
    }

    private static Color Hex(int rgb) =>
        new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
}
