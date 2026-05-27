using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_GameOver : UIPopup
{
    protected override bool CanCloseOnBackdropClick => false;

    [Header("GameOver Text Drop")]
    [SerializeField] private RectTransform gameOverTextRect;
    [SerializeField] private float textDropOffset = 600f;
    [SerializeField] private float textDropDuration = 0.75f;
    [SerializeField] private Ease textDropEase = Ease.OutBounce;

    [Header("Button Scale In")]
    [SerializeField] private RectTransform buttonRect;
    [SerializeField] private float buttonScaleDuration = 0.65f;
    [SerializeField] private Ease buttonScaleEase = Ease.OutQuad;

    private Vector2 _textRestAnchoredPosition;
    private Vector3 _buttonNormalScale = Vector3.one;
    private Sequence _enterSequence;

    public override UniTask InitializeAsync() => UniTask.CompletedTask;

    private void Awake()
    {
        CacheRestTransforms();
    }

    public override void OnShow()
    {
        base.OnShow();
        PlayEnterAnimation();
    }

    public override void OnHide()
    {
        StopEnterAnimation();
        base.OnHide();
    }

    private void OnDestroy()
    {
        StopEnterAnimation();
    }

    private void CacheRestTransforms()
    {
        if (gameOverTextRect != null)
            _textRestAnchoredPosition = gameOverTextRect.anchoredPosition;

        if (buttonRect != null)
            _buttonNormalScale = buttonRect.localScale;
    }

    private void PlayEnterAnimation()
    {
        StopEnterAnimation();
        PrepareEnterState();

        _enterSequence = DOTween.Sequence().SetUpdate(true);

        if (gameOverTextRect != null)
        {
            _enterSequence.Append(gameOverTextRect
                .DOAnchorPos(_textRestAnchoredPosition, textDropDuration)
                .SetEase(textDropEase));
        }

        if (buttonRect != null)
        {
            _enterSequence.Append(buttonRect
                .DOScale(_buttonNormalScale, buttonScaleDuration)
                .SetEase(buttonScaleEase));
        }
    }

    private void PrepareEnterState()
    {
        if (gameOverTextRect != null)
            gameOverTextRect.anchoredPosition = _textRestAnchoredPosition + Vector2.up * textDropOffset;

        SetButtonScale(Vector3.zero);
    }

    private void SetButtonScale(Vector3 scale)
    {
        if (buttonRect != null)
            buttonRect.localScale = scale;
    }

    private void StopEnterAnimation()
    {
        if (_enterSequence != null && _enterSequence.IsActive())
            _enterSequence.Kill();

        _enterSequence = null;
        ResetVisualState();
    }

    private void ResetVisualState()
    {
        if (gameOverTextRect != null)
            gameOverTextRect.anchoredPosition = _textRestAnchoredPosition;

        if (buttonRect != null)
            buttonRect.localScale = _buttonNormalScale;
    }

    public void OnClickTitle()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.LoadTitleScene();
    }
}
