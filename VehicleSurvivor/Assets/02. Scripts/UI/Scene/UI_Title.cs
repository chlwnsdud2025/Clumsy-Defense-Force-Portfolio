using DG.Tweening;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UI_Title : UIScene
{
    //TODO : 마우스 클릭시 이팩트 dotween	

    [Header("Title References")]
    [SerializeField] private RectTransform titleRect;
    [SerializeField] private TMP_Text titleText;

    [Header("Intro Position")]
    [SerializeField] private Vector2 startAnchoredPosition = new Vector2(0f, 500f);
    [SerializeField] private float targetY = -229f;

    [Header("Intro Scale")]
    [SerializeField] private Vector3 startScale = Vector3.zero;
    [SerializeField] private float targetScale = 1f;

    [Header("Intro Timing")]
    [SerializeField] private float dropDuration = 0.8f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float scaleDuration = 0.5f;

    [Header("Intro Ease")]
    [SerializeField] private Ease dropEase = Ease.OutBounce;
    [SerializeField] private Ease scaleEase = Ease.OutBounce;

    [Header("Punch Effect")]
    [SerializeField] private float punchPower = 0.15f;
    [SerializeField] private float punchDuration = 0.25f;

    [Header("Idle Floating")]
    [SerializeField] private float idleMoveY = 20f;
    [SerializeField] private float idleDuration = 1.2f;
    [SerializeField] private Ease idleEase = Ease.InOutSine;

    [Header("Start Text Blink")]
    [SerializeField] private TMP_Text startText;
    [SerializeField] private float startTextMinAlpha = 0.25f;
    [SerializeField] private float startTextBlinkDuration = 0.6f;
    [SerializeField] private Ease startTextBlinkEase = Ease.InOutSine;

    private Tween startTextBlinkTween;
    private Sequence introSeq;
    private Tween idleTween;

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
        if (titleRect == null || titleText == null)
        {
            return;
        }

        titleRect.anchoredPosition = startAnchoredPosition;
        titleRect.localScale = startScale;
        titleText.alpha = 0f;


        introSeq = DOTween.Sequence();

        introSeq.Append(titleRect
            .DOAnchorPosY(targetY, dropDuration)
            .SetEase(dropEase));
        introSeq
            .Join(titleText
            .DOFade(1f, fadeDuration));
        introSeq
            .Join(titleRect
            .DOScale(targetScale, scaleDuration)
            .SetEase(scaleEase));
        introSeq
            .Append(titleRect
            .DOPunchScale(Vector3.one * punchPower, punchDuration));
        introSeq.OnComplete(() =>
        {
            idleTween = titleRect
            .DOAnchorPosY(idleMoveY, idleDuration)
            .SetRelative()
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(idleEase);
        });
        PlayStartTextBlink();



    }
    private void PlayStartTextBlink()
    {
        if (startText == null) return;

        startText.text = "click to start";
        startText.alpha = 1f;

        startTextBlinkTween?.Kill();
        startTextBlinkTween = startText
            .DOFade(startTextMinAlpha, startTextBlinkDuration)
            .SetEase(startTextBlinkEase)
            .SetLoops(-1, LoopType.Yoyo);
    }
    private void KillTweens()
    {
        introSeq?.Kill();
        idleTween?.Kill();
        startTextBlinkTween?.Kill();


        introSeq = null;
        idleTween = null;
        startTextBlinkTween = null;
    }
    private void OnDestroy()
    {
        KillTweens();
    }

    public void OnClickStart()
    {
        GameManager.Instance.GoToCharacterSelect();
    }
    public void OnClickSetting()
    {

    }
    public void OnClickQuit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif

    }
}
