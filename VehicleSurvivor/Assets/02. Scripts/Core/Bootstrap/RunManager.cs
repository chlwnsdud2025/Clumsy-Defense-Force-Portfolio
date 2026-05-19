using UnityEngine;

public class RunManager : MonoSceneSingleton<RunManager>
{
    [Header("Databases")]
    [SerializeField] private WeaponDatabaseSO weaponDatabase;

    [Header("Runtime References")]
    [SerializeField] private Transform playerSpawnPoint;

    private IEventBus _eventBus;
    private RunContext _runContext;

    private int _killCount;
    private int _gold;
    private int _lastDisplayedSecond = -1;

    private GameObject playerInstance;
    private CharacterDefinitionSO _selectedCharacter;

    public RunContext RunContext => _runContext;

    public void StartRun(CharacterDefinitionSO selectedCharacter)
    {
        if (selectedCharacter == null)
        {
            Debug.LogError("[RunManager] selectedCharacter이 null입니다");
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
        Time.timeScale = 1f;

        ResolvePlayerInstance();
        EquipDefaultWeapon(selectedCharacter);

        PlayerRuntime playerRuntime = new PlayerRuntime(selectedCharacter);
        _runContext = new RunContext(playerRuntime);

        BindRunContext(_runContext);
        BindPlayerRuntime(playerRuntime);
        _runContext.StartRun();

        _killCount = 0;
        _gold = 0;
        _lastDisplayedSecond = -1;

        PublishHpChanged(playerRuntime.Health.CurrentHp, playerRuntime.Health.MaxHp);
        PublishExpChanged(playerRuntime.Level.CurrentExp, playerRuntime.Level.RequiredExp);
        _eventBus.Publish(new KillCountEvent(_killCount));
        _eventBus.Publish(new GoldChangedEvent(_gold));
        _eventBus.Publish(new TimeChangeEvent(_runContext.RunTime));
    }

    private void ResolvePlayerInstance()
    {
        Player player = FindFirstObjectByType<Player>();
        playerInstance = player != null ? player.gameObject : null;

        if (playerInstance == null)
        {
            Debug.LogError("[RunManager] Scene에서 Player 오브젝트를 찾지 못했습니다.");
        }
    }

    private void EquipDefaultWeapon(CharacterDefinitionSO character)
    {
        if (playerInstance == null)
        {
            Debug.LogError("[RunManager] Player가 생성되지 않아 기본 무기를 장착할 수 없습니다.");
            return;
        }

        if (weaponDatabase == null)
        {
            Debug.LogError("[RunManager] WeaponDatabaseSO가 연결되지 않았습니다.");
            return;
        }

        string defaultWeaponId = character.DefaultWeaponId;

        if (string.IsNullOrWhiteSpace(defaultWeaponId))
        {
            Debug.LogError($"[RunManager] {character.DisplayName}의 DefaultWeaponId가 비어 있습니다.");
            return;
        }

        WeaponDefinitionSO defaultWeapon = weaponDatabase.GetWeaponOrNull(defaultWeaponId);

        if (defaultWeapon == null)
            return;

        WeaponInventory weaponInventory = playerInstance.GetComponent<WeaponInventory>();
        if (weaponInventory == null)
        {
            Debug.LogError("[RunManager] PlayerPrefab에 WeaponInventory가 없습니다.");
            return;
        }

        weaponInventory.TryAddWeapon(defaultWeapon, playerInstance.transform);
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
            _eventBus.Publish(new TimeChangeEvent(_runContext.RunTime));
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
        _eventBus.Publish(new KillCountEvent(_killCount));
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
        _eventBus.Publish(new GoldChangedEvent(_gold));
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
            uiManager.ShowPopup(EPopupUIType.LevelUp, remember: false);
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
