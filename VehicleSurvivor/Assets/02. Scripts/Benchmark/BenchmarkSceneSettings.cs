using UnityEngine;

/// <summary>
/// [측정용] TestScene 에만 두는 환경 설정. 씬에 있는 동안만 플래그를 켜고 파괴 시 되돌린다.
/// 플레이어 무적 · 레벨업 팝업 차단 · (선택) 경험치 드롭 차단.
/// </summary>
public class BenchmarkSceneSettings : MonoBehaviour
{
    [Tooltip("플레이어가 데미지를 받지 않습니다.")]
    public bool playerInvincible = true;

    [Tooltip("레벨업 시 보상 선택 팝업을 띄우지 않습니다 (팝업이 뜨면 게임플레이가 멈춰 측정이 끊깁니다).")]
    public bool disableLevelUpPopup = true;

    [Tooltip("적 사망 시 경험치 구슬을 떨어뜨리지 않습니다. 구슬 처리 비용까지 제외하고 싶을 때만 켭니다.")]
    public bool disableExpDrop = false;

    [Tooltip("피격 시 데미지 텍스트를 띄우지 않습니다 (히트마다 문자열 할당 → GC 기준선 상승 방지).")]
    public bool disableDamageText = false;

    [Tooltip("적 사망 시 코인을 떨어뜨리지 않습니다.")]
    public bool disableMoneyDrop = false;

    [Header("Pooling 비교")]
    [Tooltip("켜면 데미지 텍스트 · 경험치 구슬 · 코인이 풀 대신 매번 Instantiate/Destroy 됩니다 (적용 전 재현). EnemyManager.usePooling 과 함께 끄면 '적 생명주기 전체' 비교가 됩니다.")]
    public bool bypassAuxPools = false;

    [Tooltip("좌측 상단 FPS · 몹 수 표시(FPSDisplay)를 이 씬에서 켭니다.")]
    public bool showFpsDisplay = true;

    private void Awake()
    {
        Apply();
        if (showFpsDisplay) EnsureFpsDisplay();
    }

    private void EnsureFpsDisplay()
    {
        FPSDisplay display = FindAnyObjectByType<FPSDisplay>(FindObjectsInactive.Include);
        if (display == null) display = gameObject.AddComponent<FPSDisplay>();
        display.enabled = true;
        display.gameObject.SetActive(true);
    }
    private void OnValidate() { if (Application.isPlaying) Apply(); }
    private void OnDestroy() => BenchmarkFlags.Reset();

    private void Apply()
    {
        BenchmarkFlags.PlayerInvincible = playerInvincible;
        BenchmarkFlags.DisableLevelUpPopup = disableLevelUpPopup;
        BenchmarkFlags.DisableExpDrop = disableExpDrop;
        BenchmarkFlags.DisableDamageText = disableDamageText;
        BenchmarkFlags.DisableMoneyDrop = disableMoneyDrop;
        BenchmarkFlags.BypassAuxPools = bypassAuxPools;
    }
}

/// <summary>게임 코드가 참조하는 측정용 플래그. BenchmarkSceneSettings 가 없으면 전부 false.</summary>
public static class BenchmarkFlags
{
    public static bool PlayerInvincible;
    public static bool DisableLevelUpPopup;
    public static bool DisableExpDrop;
    public static bool DisableDamageText;
    public static bool DisableMoneyDrop;
    /// <summary>데미지 텍스트 · 경험치 구슬 · 코인 풀을 우회해 매번 Instantiate/Destroy (적 생명주기 전체 풀링 비교용)</summary>
    public static bool BypassAuxPools;
    public static int AuxInstantiateCount;
    public static int AuxDestroyCount;

    public static void Reset()
    {
        PlayerInvincible = false;
        DisableLevelUpPopup = false;
        DisableExpDrop = false;
        DisableDamageText = false;
        DisableMoneyDrop = false;
        BypassAuxPools = false;
    }
}
