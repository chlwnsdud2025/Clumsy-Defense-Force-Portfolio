using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : MonoBehaviour
{
    [Header("Volume Sliders")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;

    private bool _isBinding;

    private void OnEnable()
    {
        BindSliders();
    }

    private void OnDisable()
    {
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
    }

    public void Refresh()
    {
        BindSliders();
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
        soundManager.SaveVolumeSettings();
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (_isBinding || !SoundManager.TryGetInstance(out SoundManager soundManager))
            return;

        soundManager.SetSfxVolume(value);
        soundManager.SaveVolumeSettings();

        if (value > 0f)
            soundManager.PlaySfx(ESoundId.SFX_UI_Click);
    }
}
