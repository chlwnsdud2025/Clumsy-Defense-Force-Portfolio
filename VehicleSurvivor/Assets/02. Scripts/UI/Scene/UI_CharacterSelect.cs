using UnityEngine;

public class UI_CharacterSelect : UIScene
{
    [SerializeField] private PlayerData playerdatas;
    [SerializeField] private Transform content;

    private bool _isInitialized;


    public override void OnShow()
    {
        base.OnShow();

        if (_isInitialized)
        {
            return;
        }

        _isInitialized = InitItems();

    }


    private bool InitItems()
    {
        if(playerdatas == null)
        {
            Debug.LogError("[UI_CharacterSelect] PlayerData가 할당되지 않았습니다.");
            return false;
        }
        if(content == null)
        {
            Debug.LogError("[UI_CharacterSelect] Content가 할당되지 않았습니다.");
            return false;
        }

        foreach (GameObject prefab in playerdatas.PlayerIcons)
        {
            Instantiate(prefab, content);
        }
        return true;

    }



    public void OnClickStartGame()
    {
        GameManager.Instance.GoToInGameHUD();
    }
}
