using DG.Tweening;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UI_Title : UIScene
{
    [Header("Title References")]
    [SerializeField] private RectTransform titleRect;

    [Header("Intro Scale")]
    [SerializeField] private float targetScale = 1f;
    [SerializeField] private float scaleDuration = 0.6f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    [Header("Start Text Blink")]
    [SerializeField] private TMP_Text startText;
    [SerializeField] private float startTextMinAlpha = 0.25f;
    [SerializeField] private float startTextBlinkDuration = 0.6f;
    [SerializeField] private Ease startTextBlinkEase = Ease.InOutSine;

    private Vector3 startScale = Vector3.zero;

    private Tween startTextBlinkTween;
    private Tween introScaleTween;

    public override void OnShow()
    {
        base.OnShow();
        PlayTitleIntro();
    }

    public override void OnHide()
    {
        base.OnHide();
        KillTweens();
    }

    private void PlayTitleIntro()
    {
        KillTweens();
        if (titleRect == null)
        {
            return;
        }

        titleRect.localScale = startScale;

        introScaleTween = titleRect
            .DOScale(targetScale, scaleDuration)
            .SetEase(scaleEase);

        PlayStartTextBlink();
    }

    private void PlayStartTextBlink()
    {
        if (startText == null) return;

        startText.alpha = 1f;

        startTextBlinkTween?.Kill();
        startTextBlinkTween = startText
            .DOFade(startTextMinAlpha, startTextBlinkDuration)
            .SetEase(startTextBlinkEase)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void KillTweens()
    {
        introScaleTween?.Kill();
        startTextBlinkTween?.Kill();

        introScaleTween = null;
        startTextBlinkTween = null;
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    public void OnClickStart()
    {
        PlayUIClickSfx();
        TitleSceneManager.Instance.PlayTitleCutscene();
    }

    public void OnClickSetting()
    {
        PlayUIClickSfx();

        if (UIManager.Instance == null)
            return;

        UIManager.Instance.ShowPopup(EPopupUIType.Setting, remember: false);
    }

    public void OnClickQuit()
    {
        PlayUIClickSfx();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
