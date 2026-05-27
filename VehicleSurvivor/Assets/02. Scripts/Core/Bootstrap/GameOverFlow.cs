using Cysharp.Threading.Tasks;

public sealed class GameOverFlow
{
    private UI_GameOver activePopup;
    private bool isShowing;

    public void OnGameOver(RunSession session)
    {
        ShowGameOverAsync(session).Forget();
    }

    public void OnRunFinished()
    {
        isShowing = false;
        ClosePopupIfOpen();
    }

    private async UniTaskVoid ShowGameOverAsync(RunSession session)
    {
        if (isShowing || session == null || session.State != ERunState.GameOver)
            return;

        if (!UIManager.TryGetInstance(out UIManager uiManager))
            return;

        isShowing = true;

        activePopup = await uiManager.ShowPopupAsync<UI_GameOver>(EPopupUIType.GameOver, remember: false);
        isShowing = false;
    }

    private void ClosePopupIfOpen()
    {
        if (activePopup == null)
            return;

        if (UIManager.TryGetInstance(out UIManager uiManager))
            uiManager.ClosePopup(activePopup);

        activePopup = null;
    }
}
