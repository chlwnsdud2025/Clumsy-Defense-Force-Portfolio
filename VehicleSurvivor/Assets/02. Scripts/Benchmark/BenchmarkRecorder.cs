using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;

/// <summary>
/// [측정용] 워밍업 뒤 일정 시간 동안 프레임별 지표를 기록해 CSV + 요약을 남긴다.
/// TestScene의 빈 오브젝트에 붙이고 F9(또는 인스펙터 컨텍스트 메뉴)로 시작한다.
/// 출력: {프로젝트}/Logs/Benchmark/{label}_{시각}.csv / .summary.txt
/// </summary>
public class BenchmarkRecorder : MonoBehaviour
{
    [Header("Run")]
    [Tooltip("파일명에 붙는 라벨. 예: grid_on_500")]
    public string label = "run";
    public float warmupSeconds = 10f;
    public float durationSeconds = 30f;
    public KeyCode startKey = KeyCode.F9;
    [Tooltip("씬 시작 시 자동으로 시작")]
    public bool autoStart = false;
    [Tooltip("이 값(ms)보다 긴 프레임은 에디터 정지(포커스 전환·임포트)로 보고 프레임 통계에서 제외합니다. 0이면 제외 안 함.")]
    public float outlierFrameMs = 1000f;

    [Header("Environment")]
    [Tooltip("시작 시 VSync 끄고 프레임 제한 해제 (측정값이 60에 캡되지 않게)")]
    public bool uncapFrameRate = true;

    private enum EPhase { Idle, Warmup, Recording, Done }
    private EPhase phase = EPhase.Idle;
    private float phaseTimer;

    private struct Counter
    {
        public string category, name, column;
        public bool isTime;
        public Counter(string category, string name, string column, bool isTime)
        { this.category = category; this.name = name; this.column = column; this.isTime = isTime; }
    }

    // ProfilerRecorder 카운터 → CSV 열
    private static readonly Counter[] Counters =
    {
        new Counter("Internal", "Main Thread", "mainThreadMs", true),
        new Counter("Memory",   "GC Allocated In Frame", "gcAllocBytes", false),
        new Counter("Render",   "Batches Count", "batches", false),
        new Counter("Render",   "SetPass Calls Count", "setPass", false),
        new Counter("Render",   "Draw Calls Count", "drawCalls", false),
        new Counter("Render",   "Instanced Batches Count", "instancedBatches", false),
        new Counter("Scripts",  "BehaviourUpdate", "behaviourUpdateMs", true),
        new Counter("Scripts",  "EnemyManager.ManagedUpdateLoop", "managedLoopMs", true),
        new Counter("Scripts",  "EnemyManager.UpdateGrid", "updateGridMs", true),
        new Counter("Scripts",  "EnemyManager.Separation", "separationMs", true),
        new Counter("Scripts",  "EnemyManager.BlockQuery", "blockQueryMs", true),
        new Counter("Scripts",  "EnemyManager.Spawn", "spawnMs", true),
        new Counter("Scripts",  "EnemyManager.Recycle", "recycleMs", true),
    };

    private readonly List<ProfilerRecorder> recorders = new List<ProfilerRecorder>();
    private readonly List<double[]> rows = new List<double[]>(20000);
    private readonly List<float> frameMs = new List<float>(20000);
    private double enemiesSum, checksSum;
    private int gcCollectStart, instantiateStart, destroyStart;
    private int auxInstantiateStart, auxDestroyStart;
    private StringBuilder csv;
    private bool skipFirstSample;

    private void Start()
    {
        if (uncapFrameRate)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }
        if (autoStart) Begin();
    }

    private void Update()
    {
        if (Input.GetKeyDown(startKey))
        {
            if (phase == EPhase.Warmup) { phase = EPhase.Idle; Debug.Log("[Benchmark] 워밍업 취소"); }
            else if (phase == EPhase.Recording) { Debug.Log("[Benchmark] 조기 종료 — 지금까지 기록을 저장"); Finish(); }
            else Begin();
            return;
        }

        if (phase == EPhase.Warmup)
        {
            phaseTimer += Time.unscaledDeltaTime;
            if (phaseTimer >= warmupSeconds) StartRecording();
            return;
        }

        if (phase != EPhase.Recording) return;

        phaseTimer += Time.unscaledDeltaTime;
        Sample();
        if (phaseTimer >= durationSeconds) Finish();
    }

    [ContextMenu("Begin (warmup → record)")]
    public void Begin()
    {
        if (phase == EPhase.Warmup || phase == EPhase.Recording) return;
        // 기록용 버퍼는 워밍업 시작 때 미리 잡아 기록 구간의 GC 지표를 오염시키지 않는다
        csv = new StringBuilder(16 * 1024 * 1024);
        rows.Clear(); frameMs.Clear();
        if (rows.Capacity < 20000) rows.Capacity = 20000;
        if (frameMs.Capacity < 20000) frameMs.Capacity = 20000;
        GC.Collect();
        phase = EPhase.Warmup;
        phaseTimer = 0f;
        Debug.Log($"[Benchmark] '{label}' 워밍업 {warmupSeconds:0}s 시작");
    }

    private void StartRecording()
    {
        foreach (Counter c in Counters)
            recorders.Add(ProfilerRecorder.StartNew(CategoryOf(c.category), c.name));

        enemiesSum = 0; checksSum = 0;
        skipFirstSample = true;
        gcCollectStart = GC.CollectionCount(0);
        EnemyManager em = EnemyManager.Instance;
        instantiateStart = em != null ? em.InstantiateCount : 0;
        destroyStart = em != null ? em.DestroyCount : 0;
        auxInstantiateStart = BenchmarkFlags.AuxInstantiateCount;
        auxDestroyStart = BenchmarkFlags.AuxDestroyCount;

        csv.Clear();
        csv.Append("t,frameMs,enemies,distanceChecks");
        foreach (Counter c in Counters) csv.Append(',').Append(c.column);
        csv.AppendLine();

        phase = EPhase.Recording;
        phaseTimer = 0f;
        Debug.Log($"[Benchmark] 기록 시작 ({durationSeconds:0}s)");
    }

    private void Sample()
    {
        if (skipFirstSample) { skipFirstSample = false; return; } // 레코더 시작 프레임은 제외
        EnemyManager em = EnemyManager.Instance;
        int enemies = em != null ? em.ActiveEnemies.Count : 0;
        int checks = em != null ? em.LastFrameDistanceChecks : 0;
        float ms = Time.unscaledDeltaTime * 1000f;
        frameMs.Add(ms);
        enemiesSum += enemies;
        checksSum += checks;

        double[] row = new double[Counters.Length];
        csv.Append(phaseTimer.ToString("0.000")).Append(',').Append(ms.ToString("0.000")).Append(',').Append(enemies).Append(',').Append(checks);
        for (int i = 0; i < Counters.Length; i++)
        {
            ProfilerRecorder r = recorders[i];
            double v = r.Valid ? r.LastValueAsDouble : double.NaN;
            if (Counters[i].isTime && !double.IsNaN(v)) v /= 1_000_000.0; // ns → ms
            row[i] = v;
            csv.Append(',').Append(double.IsNaN(v) ? "" : v.ToString("0.###"));
        }
        csv.AppendLine();
        rows.Add(row);
    }

    private void Finish()
    {
        phase = EPhase.Done;
        foreach (ProfilerRecorder r in recorders) r.Dispose();
        recorders.Clear();

        string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "Benchmark");
        Directory.CreateDirectory(dir);
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string basePath = Path.Combine(dir, $"{label}_{stamp}");
        File.WriteAllText(basePath + ".csv", csv.ToString());

        string summary = BuildSummary();
        File.WriteAllText(basePath + ".summary.txt", summary);
        Debug.Log($"[Benchmark] 완료 → {basePath}.csv\n{summary}");
    }

    private string BuildSummary()
    {
        EnemyManager em = EnemyManager.Instance;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"# {label}  {DateTime.Now:yyyy-MM-dd HH:mm}  Unity {Application.unityVersion}  {(Application.isEditor ? "Editor" : "Player")}");
        sb.AppendLine($"# CPU {SystemInfo.processorType} / GPU {SystemInfo.graphicsDeviceName}");
        if (em != null)
            sb.AppendLine($"# toggles  ManagedUpdate={em.useManagedUpdate}  Pooling={em.usePooling}  SpatialGrid={em.useSpatialGrid}");
        sb.AppendLine($"# aux  bypassAuxPools={BenchmarkFlags.BypassAuxPools}  damageText={!BenchmarkFlags.DisableDamageText}  exp={!BenchmarkFlags.DisableExpDrop}  money={!BenchmarkFlags.DisableMoneyDrop}");
        sb.AppendLine($"# frames {frameMs.Count}  warmup {warmupSeconds:0}s  duration {durationSeconds:0}s");
        sb.AppendLine();

        List<float> kept = new List<float>(frameMs.Count);
        int outliers = 0;
        foreach (float v in frameMs)
        {
            if (outlierFrameMs > 0f && v > outlierFrameMs) outliers++;
            else kept.Add(v);
        }
        if (outliers > 0)
            sb.AppendLine($"# {outlierFrameMs:0}ms 초과 프레임 {outliers}개 제외 (에디터 정지로 간주) — mainThreadMs 는 원본 그대로");

        if (kept.Count > 0)
        {
            // 프레임 시간: 평균 · 1% low(가장 느린 1% 프레임 평균) · 최대
            List<float> sorted = new List<float>(kept);
            sorted.Sort();
            float avg = Mean(kept);
            int onePct = Mathf.Max(1, sorted.Count / 100);
            float low1 = 0f;
            for (int i = sorted.Count - onePct; i < sorted.Count; i++) low1 += sorted[i];
            low1 /= onePct;
            sb.AppendLine($"frameMs        avg {avg:0.00} (≈{1000f / avg:0.0} fps)   1%low {low1:0.00} (≈{1000f / low1:0.0} fps)   max {sorted[sorted.Count - 1]:0.00}");
        }

        for (int i = 0; i < Counters.Length; i++)
        {
            double sum = 0, max = double.MinValue;
            int n = 0;
            foreach (double[] row in rows)
            {
                double v = row[i];
                if (double.IsNaN(v)) continue;
                sum += v; n++;
                if (v > max) max = v;
            }
            if (n == 0) { sb.AppendLine($"{Counters[i].column,-18}(counter 없음)"); continue; }
            string unit = Counters[i].isTime ? "ms" : (Counters[i].column == "gcAllocBytes" ? "B" : "");
            sb.AppendLine($"{Counters[i].column,-18}avg {sum / n:0.###} {unit}   max {max:0.###} {unit}");
        }

        sb.AppendLine($"gcCollections      {GC.CollectionCount(0) - gcCollectStart}");
        if (em != null && rows.Count > 0)
        {
            sb.AppendLine($"enemies            avg {enemiesSum / rows.Count:0.0}");
            sb.AppendLine($"distanceChecks     avg/frame {checksSum / rows.Count:0}");
            sb.AppendLine($"instantiate        {em.InstantiateCount - instantiateStart}   destroy {em.DestroyCount - destroyStart}   (기록 구간)");
            sb.AppendLine($"auxInstantiate     {BenchmarkFlags.AuxInstantiateCount - auxInstantiateStart}   auxDestroy {BenchmarkFlags.AuxDestroyCount - auxDestroyStart}   (텍스트·구슬·코인, 기록 구간)");
        }
        return sb.ToString();
    }

    private static float Mean(List<float> list)
    {
        if (list.Count == 0) return 0f;
        double s = 0;
        foreach (float v in list) s += v;
        return (float)(s / list.Count);
    }

    private static ProfilerCategory CategoryOf(string name)
    {
        switch (name)
        {
            case "Memory": return ProfilerCategory.Memory;
            case "Render": return ProfilerCategory.Render;
            case "Scripts": return ProfilerCategory.Scripts;
            default: return ProfilerCategory.Internal;
        }
    }

    private void OnGUI()
    {
        if (phase == EPhase.Idle) return;
        int enemies = EnemyManager.Instance != null ? EnemyManager.Instance.ActiveEnemies.Count : 0;
        string text;
        if (phase == EPhase.Warmup) text = $"[Benchmark] warmup {phaseTimer:0.0}/{warmupSeconds:0}s   enemies {enemies}";
        else if (phase == EPhase.Recording) text = $"[Benchmark] REC {phaseTimer:0.0}/{durationSeconds:0}s   enemies {enemies}";
        else text = "[Benchmark] done";
        float top = 10 + Screen.height * 2 / 50 + 6; // FPSDisplay 아래
        GUI.Label(new Rect(10, top, 800, 24), text);
    }
}
