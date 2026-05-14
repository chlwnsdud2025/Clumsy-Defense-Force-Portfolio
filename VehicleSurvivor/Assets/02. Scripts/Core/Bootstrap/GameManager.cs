using Cysharp.Threading.Tasks;
using System;
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
    private readonly UniTaskCompletionSource _bootCompletionSource = new UniTaskCompletionSource();

    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string inGameSceneName = "InGameScene";

    private bool _isBootCompleted;

    public GameState CurrentState { get; private set; } = GameState.Title;
    public bool IsPlaying => CurrentState == GameState.Playing;
    public bool IsBootCompleted => _isBootCompleted;

    private async void Start()
    {
        if (!ReferenceEquals(Instance, this))
            return;

        if (!await InitializeServicesAsync())
        {
            NotifyBootFailed(new InvalidOperationException("서비스 초기화에 실패했습니다."));
            return;
        }

        if (!await InitializeGameUIAsync())
        {
            NotifyBootFailed(new InvalidOperationException("UI 초기화에 실패했습니다."));
            return;
        }

        NotifyBootCompleted();
        ChangeState(GameState.Title);

        await UniTask.Yield();
    }

    public UniTask WaitForBootAsync()
    {
        if (_isBootCompleted)
            return UniTask.CompletedTask;

        return _bootCompletionSource.Task;
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
    }

    public async void LoadInGameScene()
    {
        await LoadInGameSceneAsync();
    }

    private async UniTask LoadInGameSceneAsync()
    {
        // TODO 만약 SceneLoadService를 사용했다면
        //if (!ServiceLocator.TryGet(out ISceneLoadService sceneLoadService))
        //{
        //    Debug.LogError("[GameManager] SceneLoadService가 등록되지 않아 InGameScene 씬을 로드할 수 없습니다.");
        //    return;
        //}

        //var loadComplete = await sceneLoadService.LoadSceneAsync(EGameScene.InGameScene, new SceneLoadOptions
        //{
        //    BeforeLoadAsync = () =>
        //    {
        //        ChangeState(GameState.Loading);
        //        UIManager.Instance.CloseCurrentScene();
        //        return UniTask.CompletedTask;
        //    },
        //    BeforeActivationAsync = () =>
        //    {
        //        ChangeState(GameState.Playing);
        //        UIManager.Instance.ShowScene(ESceneUIType.InGameHUD);
        //        return UniTask.CompletedTask;
        //    }
        //});

        //if (!loadComplete)
        //{
        //    Debug.LogError("[GameManager] 씬 로드를 실패 하였습니다.");
        //    return;
        //}


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

    private void NotifyBootCompleted()
    {
        if (_isBootCompleted)
            return;

        _isBootCompleted = true;
        _bootCompletionSource.TrySetResult();
    }

    private void NotifyBootFailed(Exception exception)
    {
        if (_isBootCompleted)
            return;

        _bootCompletionSource.TrySetException(exception);
    }
}