using UnityEngine;

public class UI_Setting : UIPopup
{
    [SerializeField] private SettingPanel settingPanel;

    public override void OnShow()
    {
        base.OnShow();
        settingPanel?.Refresh();
    }
}
