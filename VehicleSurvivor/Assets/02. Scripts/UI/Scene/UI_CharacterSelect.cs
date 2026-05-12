using UnityEngine;

public class UI_CharacterSelect : UIScene
{
    public void OnClickStartGame()
    {
        GameManager.Instance.StartGame();
    }
}
