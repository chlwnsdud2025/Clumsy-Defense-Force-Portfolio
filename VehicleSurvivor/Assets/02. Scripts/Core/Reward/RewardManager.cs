using System.Collections.Generic;
using UnityEngine;

public class RewardManager : MonoSceneSingleton<RewardManager>
{
    [Header("Databases")]
    [SerializeField] private WeaponDatabaseSO weaponDatabase;
    [SerializeField] private PassiveTableSO passiveTable;
    [SerializeField] private ItemTableSO itemTable;

    [SerializeField] private int rewardOptionCount = 3;

    private RunSession _runSession;
    private Player _playerInstance;
    private RewardSystem _rewardSystem;
    private readonly RewardApplier _rewardApplier = new();
    private readonly LevelUpRewardFlow _levelUpRewardFlow = new();
    private readonly ChestRewardFlow _chestRewardFlow = new();

    public RunSession RunSession => _runSession;
    public WeaponDatabaseSO WeaponDatabase => weaponDatabase;

    public void InitializeForRun(RunSession runSession, Player player)
    {
        _runSession = runSession;
        _playerInstance = player;
        ResetRunState();
        ApplyStartingLoadout();
    }

    public void ReleaseRun()
    {
        ResetRunState();
        _runSession = null;
        _playerInstance = null;
    }

    private void ResetRunState()
    {
        _levelUpRewardFlow.Reset();
        _chestRewardFlow.Reset();
    }

    public void OnLevelChanged(RunSession runSession, int levelsGained)
    {
        _levelUpRewardFlow.OnLevelChanged(runSession, levelsGained, this);
    }

    public void OnRunFinished()
    {
        _levelUpRewardFlow.OnRunFinished();
        _chestRewardFlow.OnRunFinished();
    }

    public List<RewardOption> GenerateLevelUpRewardOptions()
    {
        if (_runSession == null)
            return new List<RewardOption>();

        _rewardSystem ??= new RewardSystem(weaponDatabase, passiveTable, itemTable);
        return _rewardSystem.GenerateLevelUp(_runSession.Player, GetRewardOptionCount());
    }

    public List<RewardOption> GenerateChestRewardOptions()
    {
        if (_runSession == null)
            return new List<RewardOption>();

        _rewardSystem ??= new RewardSystem(weaponDatabase, passiveTable, itemTable);
        return _rewardSystem.GenerateChest(_runSession.Player, GetRewardOptionCount());
    }

    public bool ApplyReward(RewardOption reward)
    {
        if (_runSession == null || reward == null)
            return false;

        PlayerWeaponSpawner playerWeaponSpawner = ResolveWeaponSpawner();

        bool applied = _rewardApplier.Apply(
            _runSession.Player,
            reward,
            playerWeaponSpawner,
            _playerInstance != null ? _playerInstance.transform : null);

        if (!applied)
            return false;

        _levelUpRewardFlow.OnRewardApplied(this, _runSession);
        return true;
    }

    public void BeginChestReward()
    {
        if (_runSession == null)
            return;

        _chestRewardFlow.BeginChestReward(_runSession, this);
    }

    public bool ApplyChestReward(RewardOption reward)
    {
        if (_runSession == null || reward == null)
            return false;

        PlayerWeaponSpawner playerWeaponSpawner = ResolveWeaponSpawner();

        bool applied = _rewardApplier.Apply(
            _runSession.Player,
            reward,
            playerWeaponSpawner,
            _playerInstance != null ? _playerInstance.transform : null);

        if (!applied)
            return false;

        _chestRewardFlow.OnRewardApplied(_runSession);
        return true;
    }

    private void ApplyStartingLoadout()
    {
        if (_runSession?.Player == null)
            return;

        CharacterDefinitionSO character = _runSession.Player.Character;
        if (character == null)
            return;

        if (weaponDatabase == null)
        {
            Debug.LogError("[RewardManager] WeaponDatabaseSO가 할당되지 않았습니다.");
            return;
        }

        string startingWeaponId = character.GetStartingWeaponId();
        if (string.IsNullOrWhiteSpace(startingWeaponId))
        {
            Debug.LogError($"[RewardManager] 시작 무기 ID가 비어 있습니다: {character.DisplayName}");
            return;
        }

        WeaponDefinitionSO startingWeapon = weaponDatabase.GetWeaponOrNull(startingWeaponId);
        if (startingWeapon == null)
            return;

        if (_playerInstance == null)
        {
            Debug.LogError("[RewardManager] 플레이어가 없어 시작 무기 비주얼을 생성할 수 없습니다.");
            return;
        }

        PlayerWeaponSpawner playerWeaponSpawner = ResolveWeaponSpawner();
        if (playerWeaponSpawner == null)
        {
            Debug.LogError("[RewardManager] PlayerWeaponSpawner를 찾을 수 없습니다.");
            return;
        }

        _rewardApplier.ApplyStartingWeapon(
            _runSession.Player,
            startingWeapon,
            playerWeaponSpawner,
            _playerInstance.transform);
    }

    private PlayerWeaponSpawner ResolveWeaponSpawner()
    {
        if (_playerInstance == null)
            return null;

        if (_playerInstance.WeaponSpawner != null)
            return _playerInstance.WeaponSpawner;

        return _playerInstance.GetComponentInChildren<PlayerWeaponSpawner>(true);
    }

    private int GetRewardOptionCount()
    {
        return rewardOptionCount > 0 ? rewardOptionCount : 3;
    }
}
