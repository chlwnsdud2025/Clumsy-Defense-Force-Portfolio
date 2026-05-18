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
    private float _playTime;
    private int _lastDisplayedSecond = -1;
    private bool _isPlaying;

    private GameObject playerInstance;
    private CharacterDefinitionSO _selectedCharacter;

    public RunContext RunContext => _runContext;


    public void StartRun(CharacterDefinitionSO selectedCharacter)
    {
        if (selectedCharacter == null)
        {
            Debug.LogError("[RunManager] 선택된 캐릭터가 null입니다.");
            return;
        }

        if (_eventBus == null)
        {
            _eventBus = ServiceLocator.Get<IEventBus>();
        }
        _selectedCharacter = selectedCharacter;

        SpawnPlayer(selectedCharacter);
        EquipDefaultWeapon(selectedCharacter);


        _runContext = new RunContext(selectedCharacter);

        _killCount = 0;
        _gold = 0;
        _playTime = 0f;
        _lastDisplayedSecond = -1;
        _isPlaying = true;

        _eventBus.Publish(new HpChangedEvent(_runContext.CurrentHp, _runContext.MaxHp));
        _eventBus.Publish(new KillCountEvent(_killCount));
        _eventBus.Publish(new GoldChangedEvent(_gold));
        _eventBus.Publish(new TimeChangeEvent(_playTime));
    }
    private void SpawnPlayer(CharacterDefinitionSO character)
    {
        if (character.VisualPrefab == null)
        {
            Debug.LogError($"[RunManager] {character.DisplayName}의 CharacterPrefab이 비어 있습니다.");
            return;
        }

        Vector3 spawnPosition = playerSpawnPoint != null
            ? playerSpawnPoint.position
            : Vector3.zero;

        Quaternion spawnRotation = playerSpawnPoint != null
            ? playerSpawnPoint.rotation
            : Quaternion.identity;

        //_playerInstance = Instantiate(character.VisualPrefab, spawnPosition, spawnRotation);
    }
    private void EquipDefaultWeapon(CharacterDefinitionSO character)
    {
        if(playerInstance == null)
        {
            Debug.LogError("[RunManager] Player가 생성되지 않아 기본 무기를 장착할 수 없습니다.");
            return;
        }

        if(weaponDatabase == null)
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
        if(weaponInventory == null)
        {
            Debug.LogError("[RunManager] PlayerPrefab에 WeaponInventory가 없습니다.");
            return;
        }
        weaponInventory.TryAddWeapon(defaultWeapon, playerInstance.transform);
    }
    private void Update()
    {
        if (!_isPlaying) return;

        _playTime += Time.deltaTime;

        int currentSecond = Mathf.FloorToInt(_playTime);
        if (currentSecond != _lastDisplayedSecond)
        {
            _lastDisplayedSecond = currentSecond;
            _eventBus.Publish(new TimeChangeEvent(_playTime));
        }
    }

    public void AddKill()
    {
        if (!_isPlaying) return;

        if (_eventBus == null)
        {
            _eventBus = ServiceLocator.Get<IEventBus>();

        }

        _killCount++;
        _eventBus.Publish(new KillCountEvent(_killCount));
    }

    public void AddGold(int amount)
    {
        if (!_isPlaying) return;

        if (_eventBus == null)
        {
            _eventBus = ServiceLocator.Get<IEventBus>();
        }

        _gold += amount;
        _eventBus.Publish(new GoldChangedEvent(_gold));
    }

    public void EndRun()
    {
        _isPlaying = false;
        _runContext = null;

    }
}
