using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class UI_Setting : UIPopup
{
    [SerializeField] private SettingPanel settingPanel;

    public override async UniTask InitializeAsync()
    {
        if (settingPanel == null)
            settingPanel = GetComponentInChildren<SettingPanel>(true);
    }

    public override void OnShow()
    {
        base.OnShow();
        settingPanel?.Refresh();
        settingPanel?.PlayEnterAnimation();
    }

    public override void OnHide()
    {
        settingPanel?.StopEnterAnimation();
        base.OnHide();
    }

    private void OnDestroy()
    {
        settingPanel?.StopEnterAnimation();
    }
}
