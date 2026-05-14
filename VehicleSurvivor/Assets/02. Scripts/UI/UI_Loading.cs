using Cysharp.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UI_Loading : UIBase
{
    [Header("Progress")]
    [SerializeField] private Slider _progressBar;

    [Header("Text")]
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private string _defaultMessage = "Loading...";

    [Header("Content")]
    [SerializeField] private GameObject _contentRoot;

    [Header("Fade")]
    [SerializeField] private CanvasGroup _canvasGroup;

    private float _progress;
    private string _message;

    public float Progress => _progress;
    public string Message => _message;

    public override UniTask InitializeAsync()
    {
        EnsureCanvasGroup();
        ApplyVisibleState(1f);
        SetContentVisible(true);
        ResetView();
        return UniTask.CompletedTask;
    }

    public override void OnShow()
    {
        base.OnShow();
        transform.SetAsLastSibling();
        ResetView();
    }

    public void SetProgress(float progress)
    {
        _progress = Mathf.Clamp01(progress);
        RefreshProgress();
    }

    public void SetMessage(string message)
    {
        _message = string.IsNullOrWhiteSpace(message) ? _defaultMessage : message;
        RefreshMessage();
    }

    public void ResetView()
    {
        _progress = 0f;
        _message = _defaultMessage;
        RefreshView();
    }

    public async UniTask PlayShowAsync(LoadingFadeMode fadeMode, float duration)
    {
        EnsureCanvasGroup();
        transform.SetAsLastSibling();

        if (!ShouldFadeIn(fadeMode) || duration <= 0f)
        {
            ApplyVisibleState(1f);
            SetContentVisible(true);
            return;
        }

        SetContentVisible(false);
        ApplyVisibleState(0f);
        await FadeAlphaAsync(1f, duration);
        SetContentVisible(true);
    }

    public async UniTask PlayHideAsync(LoadingFadeMode fadeMode, float duration)
    {
        if (!gameObject.activeSelf)
            return;

        EnsureCanvasGroup();
        SetContentVisible(false);

        if (!ShouldFadeOut(fadeMode) || duration <= 0f)
        {
            ApplyHiddenState();
            return;
        }

        await FadeAlphaAsync(0f, duration);
        ApplyHiddenState();
    }

    private void RefreshView()
    {
        RefreshProgress();
        RefreshMessage();
    }

    private void EnsureCanvasGroup()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void ApplyVisibleState(float alpha)
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = Mathf.Clamp01(alpha);
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }

    private void ApplyHiddenState()
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    private void SetContentVisible(bool isVisible)
    {
        if (_contentRoot == null)
            return;

        _contentRoot.SetActive(isVisible);
    }

    private async UniTask FadeAlphaAsync(float targetAlpha, float duration)
    {
        if (_canvasGroup == null)
            return;

        float startAlpha = _canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);
            await UniTask.Yield();
        }

        _canvasGroup.alpha = Mathf.Clamp01(targetAlpha);
    }

    private bool ShouldFadeIn(LoadingFadeMode fadeMode)
    {
        return fadeMode == LoadingFadeMode.FadeInOnly || fadeMode == LoadingFadeMode.FadeInOut;
    }

    private bool ShouldFadeOut(LoadingFadeMode fadeMode)
    {
        return fadeMode == LoadingFadeMode.FadeOutOnly || fadeMode == LoadingFadeMode.FadeInOut;
    }

    private void RefreshProgress()
    {
        if (_progressBar != null)
            _progressBar.value = _progress;
    }

    private void RefreshMessage()
    {
        if (_messageText != null)
            _messageText.text = _message;
    }
}
