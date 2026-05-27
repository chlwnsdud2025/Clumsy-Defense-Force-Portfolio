using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : MonoBehaviour
{
    [Header("Volume Sliders")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;

    [Header("진입 애니메이션")]
    [SerializeField] private RectTransform tweenTarget;
    [SerializeField] private float enterDuration = 0.35f;
    [SerializeField] private Ease enterEase = Ease.OutBack;

    private bool _isBinding;
    private bool _volumeDirty;
    private Vector3 _normalScale = Vector3.one;
    private Tween _enterTween;

    private void Awake()
    {
        if (tweenTarget == null)
            tweenTarget = GetComponent<RectTransform>();

        _normalScale = tweenTarget.localScale;
    }

    private void OnEnable()
    {
        BindSliders();
    }

    private void OnDisable()
    {
        SaveVolumeSettingsIfDirty();
        StopEnterAnimation();

        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
    }

    private void OnDestroy()
    {
        StopEnterAnimation();
    }

    public void Configure(float duration, Ease ease)
    {
        enterDuration = duration;
        enterEase = ease;
    }

    public void PlayEnterAnimation()
    {
        StopEnterAnimation();

        if (tweenTarget == null)
            return;

        tweenTarget.localScale = Vector3.zero;

        _enterTween = tweenTarget
            .DOScale(_normalScale, enterDuration)
            .SetEase(enterEase)
            .SetUpdate(true);
    }

    public void StopEnterAnimation()
    {
        if (_enterTween != null && _enterTween.IsActive())
            _enterTween.Kill();

        _enterTween = null;

        if (tweenTarget != null)
            tweenTarget.localScale = _normalScale;
    }

    public void Refresh()
    {
        BindSliders();
    }

    public void SaveVolumeSettingsIfDirty()
    {
        if (!_volumeDirty || !SoundManager.TryGetInstance(out SoundManager soundManager))
            return;

        soundManager.SaveVolumeSettings();
        _volumeDirty = false;
    }

    private void BindSliders()
    {
        if (!SoundManager.TryGetInstance(out SoundManager soundManager))
            return;

        _isBinding = true;

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(soundManager.GetBgmVolume());
            musicSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
            musicSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(soundManager.GetSfxVolume());
            sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        _isBinding = false;
    }

    private void OnBgmVolumeChanged(float value)
    {
        if (_isBinding || !SoundManager.TryGetInstance(out SoundManager soundManager))
            return;

        soundManager.SetBgmVolume(value);
        _volumeDirty = true;
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (_isBinding || !SoundManager.TryGetInstance(out SoundManager soundManager))
            return;

        soundManager.SetSfxVolume(value);
        _volumeDirty = true;
    }
}
