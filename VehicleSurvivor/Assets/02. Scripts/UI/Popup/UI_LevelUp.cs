using UnityEngine;

public class UI_LevelUp : UIPopup
{
    private void Update()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Escape))
        {
            ResumeGameplay();
        }
    }

    public override void OnClickBack()
    {
        ResumeGameplay();
    }

    public void OnClickContinue()
    {
        ResumeGameplay();
    }

    private void ResumeGameplay()
    {
        RunContext runContext = RunManager.Instance?.RunContext;
        if (runContext == null)
        {
            UIManager.Instance?.ClosePopup(this);
            return;
        }

        runContext.ResumeGamePlay();
    }
}
