using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public enum EGameFlowState
{
    Title,
    CharacterSelect,
    Loading,
    InGame,
    Result

}

public class GameManager : MonoSingleton<GameManager>
{
    private readonly UniTaskCompletionSource _bootCompletionSource = new UniTaskCompletionSource();

    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string inGameSceneName = "InGameScene";

    private bool _isBootCompleted;
    private bool _isSceneChanging;

    public EGameFlowState CurrentState { get; private set; } = EGameFlowState.Title;
    public bool IsPlaying => CurrentState == EGameFlowState.InGame;
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
        ChangeState(EGameFlowState.Title);

        await UniTask.Yield();
    }

    public UniTask WaitForBootAsync()
    {
        if (_isBootCompleted)
            return UniTask.CompletedTask;

        return _bootCompletionSource.Task;
    }

    public void ChangeState(EGameFlowState newState)
    {
        CurrentState = newState;
    }

    public async void LoadInGameScene()
    {
        await LoadInGameSceneAsync();
    }

    public async void LoadTitleScene()
    {
        await LoadTitleSceneAsync();
    }

    private async UniTask LoadInGameSceneAsync()
    {
        if (_isSceneChanging)
            return;

        _isSceneChanging = true;

        try
        {
            if (!ServiceLocator.TryGet(out ISceneLoadService sceneLoadService))
            {
                Debug.LogError("[GameManager] SceneLoadService가 등록되지 않아 InGameScene 씬을 로드할 수 없습니다.");
                return;
            }

            ChangeState(EGameFlowState.Loading);

            bool loaded = await sceneLoadService.LoadSceneAsync(inGameSceneName, new SceneLoadOptions
            {
                BeforeLoadAsync = () =>
                {
                    UIManager.Instance.CloseCurrentSceneAndAllPopups();
                    SoundManager.Instance.StopBgm();
                    return UniTask.CompletedTask;
                },
                AfterSceneActivatedAsync = async () =>
                {
                    if (InGameSceneManager.TryGetInstance(out InGameSceneManager inGameSceneManager))
                        await inGameSceneManager.InitializeSceneAsync();
                }
            });

            if (!loaded)
            {
                Debug.LogError("[GameManager] InGameScene 씬 로드에 실패했습니다.");

                ChangeState(EGameFlowState.CharacterSelect);
                UIManager.Instance.ShowScene(ESceneUIType.CharacterSelect);
                SoundPlayback.PlayBgm(EBgmId.BGM_Title);
                return;
            }

            ChangeState(EGameFlowState.InGame);
            SoundPlayback.PlayBgm(EBgmId.BGM_InGame_Normal);
        }
        finally
        {
            _isSceneChanging = false;
        }
    }

    private async UniTask LoadTitleSceneAsync()
    {
        if (_isSceneChanging)
            return;

        _isSceneChanging = true;

        try
        {
            if (!ServiceLocator.TryGet(out ISceneLoadService sceneLoadService))
            {
                Debug.LogError("[GameManager] SceneLoadService가 등록되지 않아 TitleScene 씬을 로드할 수 없습니다.");
                return;
            }

            ChangeState(EGameFlowState.Loading);

            bool loaded = await sceneLoadService.LoadSceneAsync(titleSceneName, new SceneLoadOptions
            {
                LoadingMessage = "Loading...",
                BeforeLoadAsync = () =>
                {
                    if (RunManager.TryGetInstance(out RunManager runManager))
                        runManager.EndRun();

                    UIManager.Instance.CloseCurrentSceneAndAllPopups();
                    SoundManager.Instance.StopBgm();

                    return UniTask.CompletedTask;
                }
            });

            if (!loaded)
            {
                Debug.LogError("[GameManager] TitleScene 씬 로드에 실패했습니다.");
                return;
            }

            ChangeState(EGameFlowState.Title);
            SoundPlayback.PlayBgm(EBgmId.BGM_Title);

            // Title UI 표시는 TitleSceneManager.Start()에서 처리.
            // 여기서 ShowScene(Title)을 또 호출하면 중복 호출 흐름이 생길 수 있음.
        }
        finally
        {
            _isSceneChanging = false;
        }
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