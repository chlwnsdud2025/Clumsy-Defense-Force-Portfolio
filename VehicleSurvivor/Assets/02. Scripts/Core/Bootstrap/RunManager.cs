using System.Collections.Generic;
using UnityEngine;

public class RunManager : MonoSceneSingleton<RunManager>
{
    [Header("Databases")]
    [SerializeField] private WeaponDatabaseSO weaponDatabase;
    [SerializeField] private PassiveTableSO passiveTable;
    [SerializeField] private int rewardOptionCount = 3;

    [Header("Temporary Toggles")]
    [SerializeField] private bool enableLevelUpRewardPopup = false;

    private IEventBus _eventBus;
    private RunContext _runContext;
    private RewardSystem rewardSystem;
    private readonly RewardApplier rewardApplier = new();

    private int _killCount;
    private int _gold;
    private int _lastDisplayedSecond = -1;

    private Player playerInstance;
    private CharacterDefinitionSO _selectedCharacter;

    public RunContext RunContext => _runContext;

    public void StartRun(CharacterDefinitionSO selectedCharacter, Player player)
    {
        if (selectedCharacter == null)
        {
            Debug.LogError("[RunManager] selectedCharacter is null.");
            return;
        }

        if (player == null)
        {
            Debug.LogError("[RunManager] Player is null.");
            return;
        }

        if (_eventBus == null)
        {
            _eventBus = ServiceLocator.Get<IEventBus>();
        }

        if (_runContext != null)
        {
            UnbindRunContext(_runContext);
            UnbindPlayerRuntime(_runContext.Player);
        }

        _selectedCharacter = selectedCharacter;
        playerInstance = player;
        Time.timeScale = 1f;

        PlayerRuntime playerRuntime = new PlayerRuntime(selectedCharacter);
        _runContext = new RunContext(playerRuntime);
        playerInstance.BindRuntime(playerRuntime);

        EquipDefaultWeapon(selectedCharacter, playerRuntime);

        BindRunContext(_runContext);
        BindPlayerRuntime(playerRuntime);
        _runContext.StartRun();

        _killCount = 0;
        _gold = 0;
        _lastDisplayedSecond = -1;

        PublishInitialRunEvents(playerRuntime);
    }

    private void EquipDefaultWeapon(CharacterDefinitionSO character, PlayerRuntime playerRuntime)
    {
        if (playerRuntime == null)
        {
            return;
        }

        if (weaponDatabase == null)
        {
            Debug.LogError("[RunManager] WeaponDatabaseSO is not assigned.");
            return;
        }

        string startingWeaponId = ResolveStartingWeaponId(character);

        if (string.IsNullOrWhiteSpace(startingWeaponId))
        {
            Debug.LogError($"[RunManager] Starting weapon id is empty: {character.DisplayName}");
            return;
        }

        WeaponDefinitionSO defaultWeapon = weaponDatabase.GetWeaponOrNull(startingWeaponId);

        if (defaultWeapon == null)
        {
            return;
        }

        WeaponInstance defaultWeaponInstance = playerRuntime.Inventory.AddOrLevelUpWeapon(defaultWeapon);

        if (playerInstance == null)
        {
            Debug.LogError("[RunManager] Player was not spawned, so the starting weapon visual cannot be created.");
            return;
        }

        PlayerWeaponSpawner playerWeaponSpawner = playerInstance.WeaponSpawner != null
            ? playerInstance.WeaponSpawner
            : playerInstance.GetComponentInChildren<PlayerWeaponSpawner>(true);
        if (playerWeaponSpawner == null)
        {
            Debug.LogError("[RunManager] PlayerWeaponSpawner was not found under the spawned player visual.");
            return;
        }

        playerWeaponSpawner.TrySpawnWeapon(defaultWeaponInstance, playerInstance.transform);
    }

    public List<RewardOption> GenerateRewardOptions()
    {
        if (_runContext == null)
        {
            return new List<RewardOption>();
        }

        rewardSystem ??= new RewardSystem(weaponDatabase, passiveTable);
        return rewardSystem.Generate(_runContext.Player, rewardOptionCount);
    }

    public bool ApplyReward(RewardOption reward)
    {
        if (_runContext == null || reward == null)
        {
            return false;
        }

        PlayerWeaponSpawner playerWeaponSpawner = playerInstance != null
            ? playerInstance.WeaponSpawner != null
                ? playerInstance.WeaponSpawner
                : playerInstance.GetComponentInChildren<PlayerWeaponSpawner>(true)
            : null;

        bool applied = rewardApplier.Apply(
            _runContext.Player,
            reward,
            playerWeaponSpawner,
            playerInstance != null ? playerInstance.transform : null);

        if (applied)
        {
            _runContext.ResumeGamePlay();
        }

        return applied;
    }

    private static string ResolveStartingWeaponId(CharacterDefinitionSO character)
    {
        if (character == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(character.DefaultWeaponId))
        {
            return character.DefaultWeaponId.Trim();
        }

        return character.Loadout.StartingWeaponId?.Trim() ?? string.Empty;
    }

    private void Update()
    {
        if (_runContext == null)
            return;

        _runContext.Tick(Time.deltaTime);

        int currentSecond = Mathf.FloorToInt(_runContext.RunTime);
        if (currentSecond != _lastDisplayedSecond)
        {
            _lastDisplayedSecond = currentSecond;
            PublishTimeChanged(_runContext.RunTime);
        }
    }

    public void AddKill()
    {
        if (_runContext == null || !_runContext.ShouldUpdateGameplay)
            return;

        if (_eventBus == null)
        {
            _eventBus = ServiceLocator.Get<IEventBus>();
        }

        _killCount++;
        PublishKillCountChanged();
    }

    public void AddGold(int amount)
    {
        if (_runContext == null || !_runContext.ShouldUpdateGameplay)
            return;

        if (_eventBus == null)
        {
            _eventBus = ServiceLocator.Get<IEventBus>();
        }

        _gold += amount;
        PublishGoldChanged();
    }

    public void EndRun()
    {
        if (_runContext != null)
        {
            UnbindRunContext(_runContext);
            UnbindPlayerRuntime(_runContext.Player);
            _runContext.EndRun();
        }

        Time.timeScale = 1f;
        _runContext = null;
        if (playerInstance != null)
        {
            playerInstance.BindRuntime(null);
        }

        playerInstance = null;
    }

    private void BindPlayerRuntime(PlayerRuntime playerRuntime)
    {
        if (playerRuntime == null)
        {
            return;
        }

        playerRuntime.Health.OnHpChanged += PublishHpChanged;
        playerRuntime.Health.OnDied += HandlePlayerDied;
        playerRuntime.Level.OnExpChanged += PublishExpChanged;
        playerRuntime.Level.OnLevelUp += HandlePlayerLevelUp;
    }

    private void UnbindPlayerRuntime(PlayerRuntime playerRuntime)
    {
        if (playerRuntime == null)
        {
            return;
        }

        playerRuntime.Health.OnHpChanged -= PublishHpChanged;
        playerRuntime.Health.OnDied -= HandlePlayerDied;
        playerRuntime.Level.OnExpChanged -= PublishExpChanged;
        playerRuntime.Level.OnLevelUp -= HandlePlayerLevelUp;
    }

    private void BindRunContext(RunContext runContext)
    {
        if (runContext == null)
        {
            return;
        }

        runContext.OnStateChanged += HandleRunStateChanged;
    }

    private void UnbindRunContext(RunContext runContext)
    {
        if (runContext == null)
        {
            return;
        }

        runContext.OnStateChanged -= HandleRunStateChanged;
    }

    private void PublishHpChanged(float currentHp, float maxHp)
    {
        _eventBus.Publish(new HpChangedEvent(
            Mathf.RoundToInt(currentHp),
            Mathf.RoundToInt(maxHp)));
    }

    private void PublishExpChanged(int currentExp, int requiredExp)
    {
        _eventBus.Publish(new ExpChangedEvent(currentExp, requiredExp));
    }

    private void PublishInitialRunEvents(PlayerRuntime playerRuntime)
    {
        PublishHpChanged(playerRuntime.Health.CurrentHp, playerRuntime.Health.MaxHp);
        PublishExpChanged(playerRuntime.Level.CurrentExp, playerRuntime.Level.RequiredExp);
        PublishKillCountChanged();
        PublishGoldChanged();
        PublishTimeChanged(_runContext.RunTime);
    }

    private void PublishKillCountChanged()
    {
        _eventBus.Publish(new KillCountEvent(_killCount));
    }

    private void PublishGoldChanged()
    {
        _eventBus.Publish(new GoldChangedEvent(_gold));
    }

    private void PublishTimeChanged(float runTime)
    {
        _eventBus.Publish(new TimeChangeEvent(runTime));
    }

    private void HandlePlayerDied()
    {
        if (_runContext == null)
        {
            return;
        }

        _runContext.GameOver();
    }

    private void HandlePlayerLevelUp(int _)
    {
        if (_runContext == null)
        {
            return;
        }

        if (!enableLevelUpRewardPopup)
        {
            return;
        }

        _runContext.EnterRewardSelection();
    }

    private void HandleRunStateChanged(ERunState previousState, ERunState nextState)
    {
        if (nextState == ERunState.RewardSelection)
        {
            Time.timeScale = 0f;
        }
        else if (nextState == ERunState.Playing ||
                 nextState == ERunState.Ended ||
                 nextState == ERunState.GameOver ||
                 nextState == ERunState.Cleared)
        {
            Time.timeScale = 1f;
        }

        UIManager uiManager = UIManager.Instance;
        if (uiManager == null)
        {
            return;
        }

        if (nextState == ERunState.RewardSelection)
        {
            UI_LevelUp rewardPopup = uiManager.ShowPopup<UI_LevelUp>(EPopupUIType.LevelUp, remember: false);
            if (rewardPopup != null)
                //rewardPopup.ShowRewards(this, GenerateRewardOptions());

            return;
        }

        UI_LevelUp levelUpPopup = uiManager.GetCurrentPopup<UI_LevelUp>();
        if (levelUpPopup == null)
        {
            return;
        }

        if (previousState == ERunState.RewardSelection || nextState == ERunState.GameOver)
        {
            uiManager.ClosePopup(levelUpPopup);
        }
    }
}
