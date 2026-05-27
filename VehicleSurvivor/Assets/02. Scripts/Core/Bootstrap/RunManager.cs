using Cysharp.Threading.Tasks;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class RunManager : MonoSceneSingleton<RunManager>
{
    private RunSession _runSession;

    private Player playerInstance;

    private IEventBus _eventBus;

    private readonly GameOverFlow _gameOverFlow = new();

    public RunSession RunSession => _runSession;
    public WavePhaseTimer WaveTimer => _runSession?.WaveTimer;

    private void Update()
    {
        if (_runSession == null || !_runSession.ShouldUpdateGameplay)
            return;

        _runSession.Tick(Time.deltaTime);
    }

    public void StartRun(CharacterDefinitionSO selectedCharacter, Player player)
    {
        if (selectedCharacter == null)
        {
            Debug.LogError("[RunManager] 선택된 캐릭터가 없습니다.");
            return;
        }

        if (player == null)
        {
            Debug.LogError("[RunManager] 플레이어가 없습니다.");
            return;
        }

        if (_eventBus == null)
            _eventBus = ServiceLocator.Get<IEventBus>();

        ReleaseActiveRunBindings();

        PlayerRuntime playerRuntime = new PlayerRuntime(selectedCharacter);
        _runSession = new RunSession(playerRuntime);

        playerInstance = player;
        playerInstance.BindRuntime(playerRuntime);

        ActivateRunSession();
        PublishInitialHudEvents();

        if (SoundManager.TryGetInstance(out SoundManager soundManager) &&
            soundManager.TryGetComponent(out SoundBgmController bgmController))
        {
            bgmController.ResetRunState();
            bgmController.PlayInGameBgm();
        }
    }

    public void EndRun()
    {
        if (_runSession == null && playerInstance == null)
            return;

        ReleaseActiveRunBindings();

        if (_runSession != null)
            _runSession.EndRun();

        Time.timeScale = 1f;
        ApplyGameplayControl(false);
        _runSession = null;
        playerInstance = null;
    }

    protected override void OnDestroy()
    {
        EndRun();
        base.OnDestroy();
    }

    private void ReleaseActiveRunBindings()
    {
        if (_runSession != null)
        {
            UnbindRunSession(_runSession);
            UnbindPlayerRuntime(_runSession.Player);
        }

        ReleaseRunSystems();

        if (playerInstance != null)
            playerInstance.UnbindRuntime();
    }

    private void ActivateRunSession()
    {
        if (_runSession != null)
        {
            BindRunSession(_runSession);
            BindPlayerRuntime(_runSession.Player);

            _runSession.ResetStats();
            _runSession.StartRun();
        }

        InitializeRunSystems();
    }

    public void StartWavePhase(float timeLimit)
    {
        if (_runSession == null)
            return;

        _runSession.StartWavePhase(timeLimit);
    }

    public void AddKill() => _runSession?.TryAddKill();

    public void AddGold(int amount) => _runSession?.TryAddGold(amount);

    public bool CanAfford(int cost) => _runSession != null && _runSession.CanAfford(cost);

    public bool TrySpendGold(int amount) => _runSession != null && _runSession.TrySpendGold(amount);

    private void InitializeRunSystems()
    {
        if (RewardManager.TryGetInstance(out RewardManager rewardManager))
            rewardManager.InitializeForRun(_runSession, playerInstance);
        else
            Debug.LogError("[RunManager] RewardManager를 찾을 수 없어 보상 시스템을 초기화할 수 없습니다.");

        if (StageRunManager.TryGetInstance(out StageRunManager stageRunManager))
            stageRunManager.InitializeForRun();
        else
            Debug.LogWarning("[RunManager] StageRunManager를 찾을 수 없어 스테이지 페이즈를 시작하지 않습니다.");
    }

    private void ReleaseRunSystems()
    {
        if (RewardManager.TryGetInstance(out RewardManager rewardManager))
            rewardManager.ReleaseRun();

        if (StageRunManager.TryGetInstance(out StageRunManager stageRunManager))
            stageRunManager.ReleaseRun();
    }

    private void BindPlayerRuntime(PlayerRuntime playerRuntime)
    {
        if (playerRuntime == null)
            return;

        playerRuntime.Health.OnHpChanged += HandleHpChanged;
        playerRuntime.Health.OnDied += HandlePlayerDied;
        playerRuntime.Level.OnExpChanged += HandleExpChanged;
        playerRuntime.Level.OnLevelChanged += HandlePlayerLevelChanged;
        playerRuntime.Inventory.Changed += HandleInventoryChanged;
    }

    private void UnbindPlayerRuntime(PlayerRuntime playerRuntime)
    {
        if (playerRuntime == null)
            return;

        playerRuntime.Health.OnHpChanged -= HandleHpChanged;
        playerRuntime.Health.OnDied -= HandlePlayerDied;
        playerRuntime.Level.OnExpChanged -= HandleExpChanged;
        playerRuntime.Level.OnLevelChanged -= HandlePlayerLevelChanged;
        playerRuntime.Inventory.Changed -= HandleInventoryChanged;
    }

    private void BindRunSession(RunSession runSession)
    {
        if (runSession == null)
            return;

        runSession.OnStateChanged += HandleRunStateChanged;
        runSession.OnRunTimeDisplayChanged += HandleRunTimeDisplayChanged;
        runSession.OnWaveTimeDisplayChanged += HandleWaveTimeDisplayChanged;
        runSession.OnKillCountChanged += PublishKillCountChanged;
        runSession.OnGoldChanged += PublishGoldChanged;
    }

    private void UnbindRunSession(RunSession runSession)
    {
        if (runSession == null)
            return;

        runSession.OnStateChanged -= HandleRunStateChanged;
        _gameOverFlow.OnRunFinished();
        runSession.OnRunTimeDisplayChanged -= HandleRunTimeDisplayChanged;
        runSession.OnWaveTimeDisplayChanged -= HandleWaveTimeDisplayChanged;
        runSession.OnKillCountChanged -= PublishKillCountChanged;
        runSession.OnGoldChanged -= PublishGoldChanged;
    }

    private void PublishInitialHudEvents()
    {
        if (_runSession == null)
            return;

        PublishHpChanged(_runSession.Player.Health.CurrentHp, _runSession.Player.Health.MaxHp);
        PublishExpChanged(_runSession.Player.Level.CurrentExp, _runSession.Player.Level.RequiredExp);
        PublishLevelChanged(_runSession.Player.Level.Level);
        PublishKillCountChanged(_runSession.KillCount);
        PublishGoldChanged(_runSession.Gold);

        _runSession.FlushTimerDisplay();
    }

    private void HandleRunTimeDisplayChanged(float elapsed)
    {
        if (_eventBus == null)
            return;

        _eventBus.Publish(new TimeChangeEvent(elapsed));
    }

    private void HandleWaveTimeDisplayChanged(float displayTime, bool isNightmare)
    {
        if (_eventBus == null)
            return;

        _eventBus.Publish(new WaveTimeChangedEvent(displayTime, isNightmare));
    }

    private void PublishHpChanged(float currentHp, float maxHp)
    {
        if (_eventBus == null)
            return;

        _eventBus.Publish(new HpChangedEvent(
            Mathf.RoundToInt(currentHp),
            Mathf.RoundToInt(maxHp)));
    }

    private void PublishExpChanged(int currentExp, int requiredExp)
    {
        if (_eventBus == null)
            return;

        _eventBus.Publish(new ExpChangedEvent(currentExp, requiredExp));
    }

    private void PublishLevelChanged(int level)
    {
        if (_eventBus == null)
            return;

        _eventBus.Publish(new LevelChangedEvent(level));
    }

    private void PublishKillCountChanged(int killCount)
    {
        if (_eventBus == null)
            return;

        _eventBus.Publish(new KillCountEvent(killCount));
    }

    private void PublishGoldChanged(int gold)
    {
        if (_eventBus == null)
            return;

        _eventBus.Publish(new GoldChangedEvent(gold));
    }

    private void PublishInventoryChanged(EInventoryCategory category, int slotIndex)
    {
        if (_eventBus == null)
            return;

        _eventBus.Publish(new InventoryChangedEvent(category, slotIndex));
    }

    private void HandleInventoryChanged(EInventoryCategory category, int slotIndex) =>
        PublishInventoryChanged(category, slotIndex);

    private void HandleHpChanged(float currentHp, float maxHp) =>
        PublishHpChanged(currentHp, maxHp);

    private void HandleExpChanged(int currentExp, int requiredExp) =>
        PublishExpChanged(currentExp, requiredExp);

    private void HandlePlayerLevelChanged(int newLevel, int levelsGained)
    {
        PublishLevelChanged(newLevel);

        if (levelsGained > 0)
            SoundPlayback.PlaySfx(ESoundId.SFX_LevelUp);

        if (RewardManager.TryGetInstance(out RewardManager rewardManager))
            rewardManager.OnLevelChanged(_runSession, levelsGained);
    }

    private void HandlePlayerDied()
    {
        if (_runSession == null || _runSession.IsFinished)
            return;

        SoundPlayback.PlaySfx(ESoundId.SFX_Player_Death);
        ResetPlayerHitFlash();
        _runSession.GameOver();

        if (playerInstance?.Controller != null)
        {
            playerInstance.Controller.PlayDeathPresentationAsync(ShowGameOverAfterDeathPresentation).Forget();
            return;
        }

        Debug.LogWarning("[RunManager] PlayerController를 찾을 수 없어 사망 연출 없이 GameOver UI를 표시합니다.", playerInstance);
        ShowGameOverAfterDeathPresentation();
    }

    private void ShowGameOverAfterDeathPresentation()
    {
        if (_runSession == null || _runSession.State != ERunState.GameOver)
            return;

        _gameOverFlow.OnGameOver(_runSession);
    }

    private void HandleRunStateChanged(ERunState previousState, ERunState nextState)
    {
        if (nextState == ERunState.RewardSelection ||
            nextState == ERunState.Paused ||
            nextState == ERunState.GameOver)
            Time.timeScale = 0f;
        else if (nextState == ERunState.Playing ||
                 nextState == ERunState.Ended ||
                 nextState == ERunState.Cleared)
            Time.timeScale = 1f;

        ApplyGameplayControl(nextState == ERunState.Playing);

        if (nextState != ERunState.GameOver && nextState != ERunState.Ended)
            return;

        if (RewardManager.TryGetInstance(out RewardManager rewardManager))
            rewardManager.OnRunFinished();

        if (nextState == ERunState.Ended)
            _gameOverFlow.OnRunFinished();
    }

    private void ResetPlayerHitFlash() =>
        playerInstance?.GetComponentInChildren<HitFlashEffect>(true)?.ResetFlash();

    private void ApplyGameplayControl(bool enabled)
    {
        if (playerInstance?.Controller == null)
            return;

        PlayerController controller = playerInstance.Controller;
        PlayerInputs inputs = controller.GetComponent<PlayerInputs>();
        inputs?.SetGameplayControlEnabled(enabled);

        controller.LockCameraPosition = !enabled;

#if ENABLE_INPUT_SYSTEM
        PlayerInput playerInput = controller.GetComponent<PlayerInput>();
        if (playerInput == null)
            return;

        if (enabled)
            playerInput.ActivateInput();
        else
            playerInput.DeactivateInput();
#endif
    }
}
