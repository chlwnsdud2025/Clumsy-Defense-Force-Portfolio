using UnityEngine;

public class TitleSceneManager : MonoSceneSingleton<TitleSceneManager>
{
    [Header("Cutscene")]
    [SerializeField] private TitleCutsceneController titleCutsceneController;

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