using DG.Tweening;
using System;
using TMPro;
using UnityEditor;
using UnityEngine;
public class UI_Title : UIScene
{
    [SerializeField] private RectTransform titleRect;
    [SerializeField] private TMP_Text titleText;

    private Sequence introSeq;
    private Tween idleTween;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlayTitleIntro();
    }

    private void PlayTitleIntro()
    {
        introSeq?.Kill();
        idleTween?.Kill();

        titleRect.anchoredPosition = new Vector2(0, 500);
        titleRect.localScale = Vector3.zero;

        introSeq = DOTween.Sequence();

        introSeq.Append(titleRect.DOAnchorPosY(-229, 0.8f).SetEase(Ease.OutBounce));
        introSeq.Join(titleText.DOFade(1f, 0.5f));
        introSeq.Join(titleRect.DOScale(1f, 0.5f).SetEase(Ease.OutBounce));
        introSeq.Append(titleRect.DOPunchScale(Vector3.zero * 0.15f, 0.25f));
        introSeq.OnComplete(() =>
        {
            idleTween = titleRect.DOAnchorPosY(20f, 1.2f).SetRelative().SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        });


    }
    private void OnDestroy()
    {
        introSeq?.Kill();
        idleTween?.Kill();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnClickStart()
    {
        UIManager.Instance.ShowScene(ESceneUIType.InGameHUD);
        //타이틀 프리펩 지우기.??? 왜 자동으로 안보임?? CloseCachedOrTransientOnSwitch이 자동으로 실행되어서?
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
