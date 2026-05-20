using System;
using UnityEngine;

public class TitleSceneManager : MonoSceneSingleton<TitleSceneManager>
{
    [Header("Cutscene Controller")]
    [SerializeField] private TitleCutsceneController titleCutsceneController;

    [Header("Character Preview")]
    [SerializeField] private Transform previewSpawnPoint;

    private GameObject spawnedPreviewCharacter;

    public bool IsSceneInitialized { get; private set; }

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
        IsSceneInitialized = true;
    }

    protected override void OnDestroy()
    {
        ClearCharacterVisual();
        base.OnDestroy();
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

    public void ShowCharacterVisual(CharacterDefinitionSO characterDefinition)
    {
        ClearCharacterVisual();

        if (characterDefinition == null)
            return;

        if (previewSpawnPoint == null)
        {
            Debug.LogWarning("[TitleSceneManager] 캐릭터 프리뷰 SpawnPoint가 할당되지 않았습니다.");
            return;
        }

        GameObject selectedCharacterPrefab = characterDefinition.PreviewPrefab;
        if (selectedCharacterPrefab == null)
        {
            Debug.LogWarning($"[TitleSceneManager] '{characterDefinition.DisplayName}'의 PreviewPrefab이 비어 있습니다.");
            return;
        }

        spawnedPreviewCharacter = Instantiate(
            selectedCharacterPrefab,
            previewSpawnPoint.position,
            previewSpawnPoint.rotation,
            previewSpawnPoint);
    }

    public void RequestStartGame()
    {
        GameManager.Instance.LoadInGameScene();
    }

    private void GoToCharacterSelect()
    {
        GameManager.Instance.ChangeState(EGameFlowState.CharacterSelect);
        UIManager.Instance.ShowScene(ESceneUIType.CharacterSelect);
    }

    private void ClearCharacterVisual()
    {
        if (spawnedPreviewCharacter == null)
            return;

        Destroy(spawnedPreviewCharacter);
        spawnedPreviewCharacter = null;
    }
}