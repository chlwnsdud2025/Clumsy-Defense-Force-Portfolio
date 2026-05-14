using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Title,
    CharacterSelect,
    Loading,
    Playing,
    Paused,
    GameOver
}

public class GameManager : MonoSingleton<GameManager>
{
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string inGameSceneName = "InGameScene";

    public GameState CurrentState { get; private set; } = GameState.Title;
    public bool IsPlaying => CurrentState == GameState.Playing;

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
    }

    private async void Start()
    {
        if (!await InitializeServicesAsync())
            return;

        if (!await InitializeGameUIAsync())
            return;

        ChangeState(GameState.Title);
        UIManager.Instance.ShowScene(ESceneUIType.Title);

        await UniTask.Yield();
    }

    public async void LoadInGameScene()
    {
        await LoadInGameSceneAsync();
    }

    private async UniTask LoadInGameSceneAsync()
    {
        ChangeState(GameState.Loading);
        UIManager.Instance.ShowLoading();

        AsyncOperation operation = SceneManager.LoadSceneAsync(inGameSceneName);
        await operation.ToUniTask();

        ChangeState(GameState.Playing);

        UIManager.Instance.HideLoading();
        UIManager.Instance.ShowScene(ESceneUIType.InGameHUD);

        if (RunManager.TryGetInstance(out RunManager runManager))
        {
            runManager.StartRun();
        }
        else
        {
            Debug.LogError("[GameManager] InGameScene에서 RunManager를 찾지 못했습니다.");
        }
    }

    public async void LoadTitleScene()
    {
        await LoadTitleSceneAsync();
    }

    private async UniTask LoadTitleSceneAsync()
    {
        ChangeState(GameState.Loading);
        UIManager.Instance.ShowLoading();

        AsyncOperation operation = SceneManager.LoadSceneAsync(titleSceneName);
        await operation.ToUniTask();

        ChangeState(GameState.Title);

        UIManager.Instance.HideLoading();
        UIManager.Instance.ShowScene(ESceneUIType.Title);
    }

    private async UniTask<bool> InitializeServicesAsync()
    {
        if (!await ServiceBootstrapper.InitializeAsync())
        {
            Debug.LogError("[GameManager] 서비스 초기화에 실패했습니다.");
            return false;
        }

        return true;
    }

    private async UniTask<bool> InitializeGameUIAsync()
    {
        bool initialized = await UIManager.Instance.InitializeAsync();
        if (!initialized)
        {
            Debug.LogError("[GameManager] UI 초기화에 실패했습니다.");
            return false;
        }

        return true;
    }
}