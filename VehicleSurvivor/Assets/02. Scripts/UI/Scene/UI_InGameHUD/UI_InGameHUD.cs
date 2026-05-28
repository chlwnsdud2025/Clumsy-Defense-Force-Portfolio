using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_InGameHUD : UIScene
{
    [Header("Player UI")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Game Info UI")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI chestCostText;
    [SerializeField] private TextMeshProUGUI killCountText;
    [SerializeField] private TextMeshProUGUI totalTimerText;
    [SerializeField] private TextMeshProUGUI waveTimerText;

    [Header("Wave Timer")]
    [SerializeField] private RectTransform waveTimerRect;
    [SerializeField] private Color waveTimerNormalTextColor = Color.white;
    [SerializeField] private Color waveTimerNightmareTextColor = new(1f, 0.27f, 0.27f);
    [SerializeField] private float waveTimerNightmarePunchPower = 0.15f;
    [SerializeField] private float waveTimerStyleTweenDuration = 0.3f;

    [Header("Boss Health UI")]
    [SerializeField] private BossHealthBarController bossHealthBarController;

    [Header("Loadout UI")]
    [SerializeField] private Transform weaponSlotRoot;
    [SerializeField] private LoadoutSlot weaponSlotPrefab;
    [SerializeField] private Transform badgeSlotRoot;
    [SerializeField] private LoadoutSlot badgeSlotPrefab;
    [SerializeField] private Transform itemSlotRoot;
    [SerializeField] private LoadoutSlot itemSlotPrefab;

    [Header("HP Tween")]
    [SerializeField] private float hpTweenDuration = 0.25f;
    [SerializeField] private Ease hpTweenEase = Ease.OutQuad;

    [Header("EXP Tween")]
    [SerializeField] private float expTweenDuration = 0.25f;
    [SerializeField] private Ease expTweenEase = Ease.OutQuad;

    [Header("HP Warning")]
    [SerializeField] private float hpWarningThreshold = 0.2f;
    [SerializeField] private float hpWarningFadeDuration = 0.35f;
    [SerializeField] private float hpWarningTargetAlpha = 0.35f;
    [SerializeField] private Ease hpWarningEase = Ease.Linear;

    private readonly List<LoadoutSlot> _weaponSlots = new();
    private readonly List<LoadoutSlot> _badgeSlots = new();
    private readonly List<LoadoutSlot> _itemSlots = new();

    private Tween hpTween;
    private Tween expTween;
    private Tween hpWarningTween;
    private Tween waveTimerStyleTween;
    private Tween waveTimerPunchTween;

    private IEventBus _eventBus;

    private bool _isSubscribed;
    private bool _isWaveNightmare;

    private void Awake()
    {
        if (waveTimerRect == null && waveTimerText != null)
            waveTimerRect = waveTimerText.rectTransform;
    }

    public async override UniTask InitializeAsync()
    {
        EnsureSlotCount(_weaponSlots, weaponSlotRoot, weaponSlotPrefab, PlayerInventory.DefaultMaxWeaponSlots);
        EnsureSlotCount(_badgeSlots, badgeSlotRoot, badgeSlotPrefab, PlayerInventory.DefaultMaxBadgeSlots);
        EnsureSlotCount(_itemSlots, itemSlotRoot, itemSlotPrefab, PlayerInventory.DefaultMaxItemSlots);
        ClearAllLoadoutSlots();
        InitializeBossHealthBar();

        await UniTask.Yield();
    }

    public override void OnShow()
    {
        base.OnShow();
        SubscribeEvents();
    }

    public override void OnHide()
    {
        UnsubscribeEvents();
        bossHealthBarController?.ResetAll();
        KillTweens();
        base.OnHide();
    }

    public void InitializeForCharacter(CharacterDefinitionSO character)
    {
        if (character == null)
        {
            UpdateHpGauge(0, 1);
            UpdateExpGauge(0, 1);
            UpdateLevelUI(1);
            UpdateGoldUI(0);
            UpdateChestCostUI(0);
            UpdateKillUI(0);
            UpdateTotalTimerUI(0f);
            UpdateWaveTimerUI(0f, false);
            ClearAllLoadoutSlots();
            bossHealthBarController?.ResetAll();
            return;
        }

        int maxHp = Mathf.RoundToInt(character.Combat.MaxHealth);
        UpdateHpGauge(maxHp, maxHp);

        UpdateExpGauge(0, 1);
        UpdateLevelUI(1);
        UpdateGoldUI(0);
        RefreshChestCostUI();
        UpdateKillUI(0);
        UpdateTotalTimerUI(0f);
        UpdateWaveTimerUI(0f, false);
        ClearAllLoadoutSlots();
        InitializeBossHealthBar();
    }

    private void InitializeBossHealthBar()
    {
        if (bossHealthBarController == null)
            return;

        bossHealthBarController.Initialize();
    }

    private void SubscribeEvents()
    {
        if (_isSubscribed)
            return;

        if (_eventBus == null)
            _eventBus = ServiceLocator.Get<IEventBus>();

        _eventBus.Subscribe<HpChangedEvent>(OnHpChanged);
        _eventBus.Subscribe<ExpChangedEvent>(OnExpChanged);
        _eventBus.Subscribe<LevelChangedEvent>(OnLevelChanged);
        _eventBus.Subscribe<KillCountEvent>(OnKillChanged);
        _eventBus.Subscribe<TimeChangeEvent>(OnTimeChanged);
        _eventBus.Subscribe<WaveTimeChangedEvent>(OnWaveTimeChanged);
        _eventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
        _eventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
        _eventBus.Subscribe<BossAppearedEvent>(OnBossAppeared);
        _eventBus.Subscribe<BossHpChangedEvent>(OnBossHpChanged);
        _eventBus.Subscribe<BossDefeatedEvent>(OnBossDefeated);

        if (StageRunManager.TryGetInstance(out StageRunManager stageRunManager))
        {
            stageRunManager.OnChestCostChanged += RefreshChestCostUI;
            RefreshChestCostUI();
        }

        _isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!_isSubscribed)
            return;

        if (_eventBus != null)
        {
            _eventBus.Unsubscribe<HpChangedEvent>(OnHpChanged);
            _eventBus.Unsubscribe<ExpChangedEvent>(OnExpChanged);
            _eventBus.Unsubscribe<LevelChangedEvent>(OnLevelChanged);
            _eventBus.Unsubscribe<KillCountEvent>(OnKillChanged);
            _eventBus.Unsubscribe<TimeChangeEvent>(OnTimeChanged);
            _eventBus.Unsubscribe<WaveTimeChangedEvent>(OnWaveTimeChanged);
            _eventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
            _eventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
            _eventBus.Unsubscribe<BossAppearedEvent>(OnBossAppeared);
            _eventBus.Unsubscribe<BossHpChangedEvent>(OnBossHpChanged);
            _eventBus.Unsubscribe<BossDefeatedEvent>(OnBossDefeated);
        }

        if (StageRunManager.TryGetInstance(out StageRunManager stageRunManager))
            stageRunManager.OnChestCostChanged -= RefreshChestCostUI;

        _isSubscribed = false;
    }

    private void RefreshChestCostUI()
    {
        int cost = StageRunManager.TryGetInstance(out StageRunManager stageRunManager)
            ? stageRunManager.CurrentChestCost
            : 0;

        UpdateChestCostUI(cost);
    }

    public void UpdateHpGauge(int current, int max)
    {
        float ratio = max > 0 ? (float)current / max : 0f;
        ratio = Mathf.Clamp01(ratio);

        if (hpSlider != null)
        {
            hpTween?.Kill();
            hpTween = null;

            if (current <= 0)
                hpSlider.value = 0f;
            else
            {
                hpTween = hpSlider
                    .DOValue(ratio, hpTweenDuration)
                    .SetEase(hpTweenEase);
            }
        }

        if (hpText != null)
            hpText.text = $"{current} / {max}";

        if (current <= 0)
            StopWarningHp();
        else if (ratio < hpWarningThreshold)
            StartWarningHp();
        else
            StopWarningHp();
    }

    public void UpdateExpGauge(int current, int max)
    {
        float ratio = max > 0 ? (float)current / max : 0f;
        ratio = Mathf.Clamp01(ratio);

        if (expSlider != null)
        {
            expTween?.Kill();
            expTween = expSlider
                .DOValue(ratio, expTweenDuration)
                .SetEase(expTweenEase);
        }
    }

    public void UpdateLevelUI(int level)
    {
        if (levelText != null)
            levelText.text = $"Lv. {level}";
    }

    public void UpdateGoldUI(int gold)
    {
        if (goldText != null)
            goldText.text = gold.ToString("N0");
    }

    public void UpdateChestCostUI(int cost)
    {
        if (chestCostText != null)
            chestCostText.text = cost.ToString("N0");
    }

    public void UpdateKillUI(int kills)
    {
        if (killCountText != null)
            killCountText.text = kills.ToString("N0");
    }

    public void UpdateTotalTimerUI(float elapsed)
    {
        if (totalTimerText == null)
            return;

        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);
        totalTimerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void UpdateWaveTimerUI(float displayTime, bool isNightmare)
    {
        if (waveTimerText == null)
            return;

        float clamped = Mathf.Max(0f, displayTime);
        int minutes = Mathf.FloorToInt(clamped / 60f);
        int seconds = Mathf.FloorToInt(clamped % 60f);
        waveTimerText.text = isNightmare
            ? $"+{minutes:00}:{seconds:00}"
            : $"{minutes:00}:{seconds:00}";

        ApplyWaveTimerStyle(isNightmare);
    }

    private void ApplyWaveTimerStyle(bool isNightmare)
    {
        if (_isWaveNightmare == isNightmare)
            return;

        bool enteringNightmare = isNightmare && !_isWaveNightmare;
        _isWaveNightmare = isNightmare;

        waveTimerStyleTween?.Kill();
        waveTimerPunchTween?.Kill();

        Color targetTextColor = isNightmare ? waveTimerNightmareTextColor : waveTimerNormalTextColor;

        if (waveTimerText != null)
        {
            waveTimerStyleTween = waveTimerText
                .DOColor(targetTextColor, waveTimerStyleTweenDuration)
                .SetEase(Ease.OutQuad);
        }

        if (enteringNightmare && waveTimerRect != null)
        {
            waveTimerRect.localScale = Vector3.one;
            waveTimerPunchTween = waveTimerRect
                .DOPunchScale(Vector3.one * waveTimerNightmarePunchPower, waveTimerStyleTweenDuration);
        }
        else if (waveTimerRect != null)
        {
            waveTimerRect.localScale = Vector3.one;
        }
    }

    private void UpdateLoadoutSlot(EInventoryCategory category, int slotIndex)
    {
        if (!RunManager.TryGetInstance(out RunManager runManager))
            return;

        PlayerInventory inventory = runManager.RunSession?.Player?.Inventory;

        if (inventory == null)
        {
            Debug.LogWarning("[UI_InGameHUD] Player Inventoryê°€ ì¡´ìž¬?˜ì? ?Šì•„ LoadoutSlot???…ë°?´íŠ¸?????†ìŠµ?ˆë‹¤.");
            return;
        }

        switch (category)
        {
            case EInventoryCategory.Weapon:
                UpdateWeaponSlot(inventory, slotIndex);
                break;
            case EInventoryCategory.Badge:
                UpdateBadgeSlot(inventory, slotIndex);
                break;
            case EInventoryCategory.Item:
                UpdateItemSlot(inventory, slotIndex);
                break;
        }
    }

    private void OnHpChanged(HpChangedEvent e) => UpdateHpGauge(e.Current, e.Max);
    private void OnExpChanged(ExpChangedEvent e) => UpdateExpGauge(e.Current, e.Required);
    private void OnLevelChanged(LevelChangedEvent e) => UpdateLevelUI(e.Level);
    private void OnKillChanged(KillCountEvent e) => UpdateKillUI(e.KillCount);
    private void OnTimeChanged(TimeChangeEvent e) => UpdateTotalTimerUI(e.Time);
    private void OnWaveTimeChanged(WaveTimeChangedEvent e) => UpdateWaveTimerUI(e.DisplayTime, e.IsNightmare);
    private void OnGoldChanged(GoldChangedEvent e) => UpdateGoldUI(e.Gold);
    private void OnInventoryChanged(InventoryChangedEvent e) => UpdateLoadoutSlot(e.Category, e.SlotIndex);
    private void OnBossAppeared(BossAppearedEvent e) => bossHealthBarController?.OnBossAppeared(e);
    private void OnBossHpChanged(BossHpChangedEvent e) => bossHealthBarController?.OnBossHpChanged(e);
    private void OnBossDefeated(BossDefeatedEvent e) => bossHealthBarController?.OnBossDefeated(e);

    private void UpdateWeaponSlot(PlayerInventory inventory, int slotIndex)
    {
        if (!TryGetSlot(_weaponSlots, inventory.MaxWeaponSlots, slotIndex, out LoadoutSlot slot))
            return;

        WeaponInstance weapon = inventory.Weapons[slotIndex];
        slot.Bind(weapon.Definition.Icon, weapon.IsEvolution ? "EVO" : $"LV {weapon.CurrentLevel}");
    }

    private void UpdateBadgeSlot(PlayerInventory inventory, int slotIndex)
    {
        if (!TryGetSlot(_badgeSlots, inventory.MaxBadgeSlots, slotIndex, out LoadoutSlot slot))
            return;

        BadgeInstance badge = inventory.Badges[slotIndex];
        slot.Bind(badge.Definition.Icon, badge.CurrentLevel);

    }

    private void UpdateItemSlot(PlayerInventory inventory, int slotIndex)
    {
        if (!TryGetSlot(_itemSlots, inventory.MaxItemSlots, slotIndex, out LoadoutSlot slot))
            return;

        ItemInstance item = inventory.Items[slotIndex];
        int displayValue = item.StackCount > 1 ? item.StackCount : 0;
        slot.Bind(item.Definition.Icon, displayValue);
    }

    private static bool TryGetSlot(
        List<LoadoutSlot> slots,
        int maxSlotCount,
        int slotIndex,
        out LoadoutSlot slot)
    {
        slot = null;

        if (slotIndex < 0 || slotIndex >= maxSlotCount || slotIndex >= slots.Count)
            return false;

        slot = slots[slotIndex];
        return slot != null;
    }

    private void EnsureSlotCount(List<LoadoutSlot> pool, Transform root, LoadoutSlot prefab, int maxCount)
    {
        if (root == null)
            return;

        maxCount = Mathf.Max(0, maxCount);

        pool.RemoveAll(slot => slot == null);

        for (int i = 0; i < root.childCount; i++)
        {
            if (!root.GetChild(i).TryGetComponent(out LoadoutSlot existingSlot))
                continue;

            if (!pool.Contains(existingSlot))
                pool.Add(existingSlot);
        }

        while (pool.Count < maxCount)
        {
            if (prefab == null)
            {
                Debug.LogError($"[UI_InGameHUD] {root.name}??Loadout ?¬ë¡¯ ?„ë¦¬?¹ì´ ? ë‹¹?˜ì? ?Šì•˜?µë‹ˆ??");
                break;
            }

            LoadoutSlot slot = Instantiate(prefab, root);
            pool.Add(slot);
        }

        for (int i = 0; i < pool.Count; i++)
        {
            LoadoutSlot slot = pool[i];
            bool isWithinMax = i < maxCount;
            slot.gameObject.SetActive(isWithinMax);

            if (!isWithinMax)
                continue;

            slot.ClearEmpty();
        }
    }

    private void ClearAllLoadoutSlots()
    {
        ClearEmptySlots(_weaponSlots, PlayerInventory.DefaultMaxWeaponSlots);
        ClearEmptySlots(_badgeSlots, PlayerInventory.DefaultMaxBadgeSlots);
        ClearEmptySlots(_itemSlots, PlayerInventory.DefaultMaxItemSlots);
    }

    private void ClearEmptySlots(List<LoadoutSlot> slots, int maxSlotCount)
    {
        int slotCount = Mathf.Min(maxSlotCount, slots.Count);

        for (int i = 0; i < slotCount; i++)
            slots[i].ClearEmpty();
    }

    private void StartWarningHp()
    {
        if (hpText == null)
            return;

        if (hpWarningTween != null && hpWarningTween.IsActive())
            return;

        hpWarningTween = hpText
            .DOFade(hpWarningTargetAlpha, hpWarningFadeDuration)
            .SetEase(hpWarningEase)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopWarningHp()
    {
        hpWarningTween?.Kill();
        hpWarningTween = null;

        if (hpText != null)
            hpText.alpha = 1f;
    }

    private void KillTweens()
    {
        hpTween?.Kill();
        expTween?.Kill();
        hpWarningTween?.Kill();
        waveTimerStyleTween?.Kill();
        waveTimerPunchTween?.Kill();

        hpTween = null;
        expTween = null;
        hpWarningTween = null;
        waveTimerStyleTween = null;
        waveTimerPunchTween = null;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (UIManager.Instance == null)
            return;

        UI_Pause pausePopup = UIManager.Instance.GetCurrentPopup<UI_Pause>();
        if (pausePopup != null)
        {
            UIManager.Instance.ClosePopup(pausePopup);
            return;
        }

        TryOpenPausePopup();
    }

    public void OnClickPausePopup()
    {
        PlayUIClickSfx();
        TryOpenPausePopup();
    }

    private void TryOpenPausePopup()
    {
        if (!RunManager.TryGetInstance(out RunManager runManager))
            return;

        if (runManager.RunSession == null || runManager.RunSession.State != ERunState.Playing)
            return;

        if (UIManager.Instance == null)
            return;

        if (UIManager.Instance.GetCurrentPopup<UIPopup>() != null)
            return;

        UIManager.Instance.ShowPopup(EPopupUIType.Pause, remember: false);
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        KillTweens();
    }
}
