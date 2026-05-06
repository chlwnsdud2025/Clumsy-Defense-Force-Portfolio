using UnityEngine;

public class UI_InGameHUD : UIScene
{
    
    public override void Show()
    {
        base.Show();
    }
    public void OnClickShowSelectCard()
    {
        UIManager.Instance.ShowPopup(EPopupUIType.SelectCard);
    }
}
