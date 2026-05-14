using System;
using UnityEngine;

public class TitleSceneManager : MonoSceneSingleton<TitleSceneManager>
{
    [Header("Cutscene")]
    [SerializeField] private TitleCutsceneController titleCutsceneController;

    private async void Start()
    {
        if (!GameManager.TryGetInstance(out GameManager gameManager))
        {
            Debug.LogError("[TitleSceneManager] GameManager Instance를 찾을 수 없어 Title UI를 표시할 수 없습니다.");
            return;
        }

        try
        {
            await gameManager.WaitForBootAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TitleSceneManager] GameManager 초기화 완료를 기다리는 중 오류가 발생했습니다: {ex.Message}");
            return;
        }

        if (!UIManager.HasInstance)
        {
            Debug.LogError("[TitleSceneManager] UIManager Instance를 찾을 수 없어 Title UI를 표시할 수 없습니다.");
            return;
        }

        UIManager.Instance.ShowScene(ESceneUIType.Title);
    }

    public void PlayTitleCutscene()
    {
        if (titleCutsceneController == null)
        {
            Debug.LogError("[TitleSceneManager] TitleCutsceneController가 할당되지 않았습니다.");
            return;
        }

        UIManager.Instance.CloseCurrentScene();
        titleCutsceneController.Play(GoToCharacterSelect);
    }

    private void GoToCharacterSelect()
    {
        GameManager.Instance.ChangeState(GameState.CharacterSelect);
        UIManager.Instance.ShowScene(ESceneUIType.CharacterSelect);
    }

    public void RequestStartGame()
    {
        GameManager.Instance.LoadInGameScene();
    }
}