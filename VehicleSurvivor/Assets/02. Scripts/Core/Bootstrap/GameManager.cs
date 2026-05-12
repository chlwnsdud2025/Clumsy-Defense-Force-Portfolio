using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
public enum GameState
{
    Title,
    CharacterSelect,
    Playing,
    Paused,
    GameOver
}
public class GameManager : MonoSingleton<GameManager>
{
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
    public void GoToCharacterSelect()
    {
        ChangeState(GameState.CharacterSelect);
        UIManager.Instance.ShowScene(ESceneUIType.CharacterSelect);
    }

    public void GoToInGameHUD()
    {
        ChangeState(GameState.Playing);
        UIManager.Instance.ShowScene(ESceneUIType.InGameHUD);
        RunManager.Instance.StartRun();
    }
    private async UniTask<bool> InitializeServicesAsync()
    {
        if (!await ServiceBootstrapper.InitializeAsync())
        {
            Debug.LogError("[GameManager] 서비스 초기화에 실패했습니다. 콘솔 로그를 확인하세요.");
            return false;
        }

        return true;
    }

    private async UniTask<bool> InitializeGameUIAsync()
    {
        Debug.Log("[GameManager] UI를 준비하는 중입니다.");

        bool initialized = await UIManager.Instance.InitializeAsync();
        if (!initialized)
        {
            Debug.LogError("[GameManager] UI 초기화에 실패했습니다. 콘솔 로그를 확인하세요.");
            return false;
        }

        return true;
    }
}
