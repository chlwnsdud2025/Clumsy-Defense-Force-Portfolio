using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class InGameSceneManager : MonoSceneSingleton<InGameSceneManager>
{
    [Tooltip("스폰될 플레이어 프리팹(Player 컴포넌트 포함)")]
    [SerializeField] private Player playerPrefab;

    [Tooltip("플레이어가 스폰될 위치")]
    [SerializeField] private Transform playerSpawnPoint;

    private Player spawnedPlayer;

    public bool IsSceneInitialized { get; private set; }

    private bool isInitializingScene;

    private async void Start()
    {
        if (!GameManager.TryGetInstance(out GameManager gameManager))
        {
            Debug.LogError("[InGameSceneManager] GameManager를 찾을 수 없어 인게임 세션을 시작할 수 없습니다.");
            return;
        }

        try
        {
            await gameManager.WaitForBootAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[InGameSceneManager] 부트 대기 중 오류: {ex.Message}");
            return;
        }

        if (!UIManager.HasInstance)
        {
            Debug.LogError("[InGameSceneManager] UIManager를 찾을 수 없어 인게임 세션을 시작할 수 없습니다.");
            return;
        }

        InitializeSceneAsync().Forget();
    }

    protected override void OnDestroy()
    {
        DestroySpawnedPlayer();
        base.OnDestroy();
    }

    public async UniTask InitializeSceneAsync()
    {
        if (IsSceneInitialized)
            return;

        while (isInitializingScene)
            await UniTask.Yield(PlayerLoopTiming.Update);

        if (IsSceneInitialized)
            return;

        isInitializingScene = true;

        try
        {
            if (!ServiceLocator.TryGet(out ICharacterService characterService))
            {
                Debug.LogError("[InGameSceneManager] ICharacterService를 찾을 수 없어 인게임 초기화를 진행할 수 없습니다.");
                return;
            }

            CharacterDefinitionSO selectedCharacter = characterService.SelectedCharacter;
            if (selectedCharacter == null)
            {
#if UNITY_EDITOR
                if (playerPrefab != null)
                    selectedCharacter = playerPrefab.DebugCharacterDefinition;
#endif
                if (selectedCharacter == null)
                    selectedCharacter = characterService.DefaultCharacter;
            }

            if (selectedCharacter == null)
            {
                Debug.LogError("[InGameSceneManager] 선택된 캐릭터가 없어 인게임 초기화를 진행할 수 없습니다.");
                return;
            }

            // InGameHUD UI 출력 및 초기화
            UI_InGameHUD hud = UIManager.Instance.ShowScene<UI_InGameHUD>(ESceneUIType.InGameHUD);
            if (hud != null)
                hud.InitializeForCharacter(selectedCharacter);

            // 캐릭터 스폰
            bool spawned = await TrySpawnSelectedCharacterAsync(selectedCharacter);
            if (!spawned)
            {
                return;
            }

            if (!RunManager.TryGetInstance(out RunManager runManager))
            {
                Debug.LogError("[InGameSceneManager] RunManager를 찾을 수 없어 StartRun을 호출할 수 없습니다.");
                return;
            }

            runManager.StartRun(selectedCharacter, spawnedPlayer);

            IsSceneInitialized = true;
        }
        finally
        {
            isInitializingScene = false;
        }
    }

    private async UniTask<bool> TrySpawnSelectedCharacterAsync(CharacterDefinitionSO selected)
    {
        if (selected == null)
            return false;

        if (playerPrefab == null)
        {
            Debug.LogError("[InGameSceneManager] Player 프리팹이 할당되어 있지 않습니다.");
            return false;
        }

        DestroySpawnedPlayer();
        DestroyAllPlayerInstances();
        DisableAudioListenersOnSceneCameras();
        await UniTask.Yield();

        Transform spawn = playerSpawnPoint != null ? playerSpawnPoint : transform;
        spawnedPlayer = Instantiate(playerPrefab, spawn.position, spawn.rotation);

        if (spawnedPlayer == null)
        {
            Debug.LogError("[InGameSceneManager] Player 인스턴스화에 실패했습니다.");
            return false;
        }

        await spawnedPlayer.InitializeVisualAsync(selected);
        WireEnemyManagerToPlayer(spawnedPlayer);
        return true;
    }

    /// <summary>
    /// EnemyManager가 추적할 Player Transform을 연결합니다.
    /// </summary>
    private void WireEnemyManagerToPlayer(Player player)
    {
        if (player == null)
            return;

        Transform playerFollowTransform = player.FollowTransform;
        if (playerFollowTransform == player.transform)
            Debug.LogWarning("[InGameSceneManager] PlayerController를 찾을 수 없어 EnemyManager에 Player.transform을 연결합니다.");

        if (EnemyManager.Instance == null)
        {
            Debug.LogWarning("[InGameSceneManager] EnemyManager.Instance가 없어 playerTransform을 갱신하지 못했습니다.");
            return;
        }

        EnemyManager.Instance.playerTransform = playerFollowTransform;
    }

    private void DestroySpawnedPlayer()
    {
        if (spawnedPlayer == null)
            return;

        Destroy(spawnedPlayer.gameObject);
        spawnedPlayer = null;
    }

    private void DestroyAllPlayerInstances()
    {
        Player[] players = FindObjectsByType<Player>(FindObjectsInactive.Include);

        for (int i = 0; i < players.Length; i++)
        {
            Player p = players[i];
            if (p != null)
                Destroy(p.gameObject);
        }
    }

    /// <summary>
    /// 플레이어 스폰 전에 씬에 남아 있는 카메라의 AudioListener를 끕니다.
    /// 스폰되는 플레이어 카메라 리스너만 활성 상태로 남기기 위함입니다.
    /// </summary>
    private void DisableAudioListenersOnSceneCameras()
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include);

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam == null)
                continue;

            if (cam.TryGetComponent(out AudioListener listener))
                listener.enabled = false;
        }
    }
}
