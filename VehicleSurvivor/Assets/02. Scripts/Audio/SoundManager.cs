using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoSingleton<SoundManager>
{
    private const string BgmVolumeParam = "BgmVolume";
    private const string SfxVolumeParam = "SfxVolume";

    [Header("Data")]
    [SerializeField] private SoundDatabaseSO soundDatabase;

    [Header("Mixer (optional)")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Pool")]
    [SerializeField] private int sfxPoolSize = 22;
    [SerializeField] private float bgmCrossFadeDuration = 0.8f;

    private readonly Dictionary<ESoundId, float> _sfxCooldownUntil = new();

    private AudioSource _bgmSource;
    private AudioSourcePool _sfxPool;
    private EBgmId _currentBgmId = EBgmId.None;
    private Tween _bgmFadeTween;

    private float _bgmVolume = SoundVolumeSettings.DefaultVolume;
    private float _sfxVolume = SoundVolumeSettings.DefaultVolume;

    private bool _isReady;

    public SoundDatabaseSO Database => soundDatabase;
    public EBgmId CurrentBgmId => _currentBgmId;

    protected override void Awake()
    {
        base.Awake();
        if (!ReferenceEquals(Instance, this))
            return;

        TryInitialize();
    }

    public void PlaySfx(ESoundId id, float volumeScale = 1f)
    {
        if (!_isReady || id == ESoundId.None || soundDatabase == null)
            return;

        if (!soundDatabase.TryGetSfx(id, out SfxEntry entry))
            return;

        if (!CanPlaySfx(id, entry.CooldownSeconds))
            return;

        if (!entry.TryGetClip(out AudioClip clip))
            return;

        float pitch = Random.Range(entry.PitchMin, entry.PitchMax);
        float volume = entry.Volume * volumeScale * _sfxVolume;

        if (_sfxPool == null)
            return;

        _sfxPool.TryPlay(clip, volume, pitch, entry.Priority, out _);
        MarkSfxPlayed(id, entry.CooldownSeconds);
    }

    public void PlayBgm(EBgmId id, bool crossFade = true)
    {
        if (!_isReady || id == EBgmId.None || soundDatabase == null)
            return;

        if (_currentBgmId == id && _bgmSource != null && _bgmSource.isPlaying)
            return;

        if (!soundDatabase.TryGetBgm(id, out BgmEntry entry) || entry.Clip == null)
            return;

        _currentBgmId = id;
        float targetVolume = entry.Volume * _bgmVolume;

        _bgmFadeTween?.Kill();

        if (_bgmSource == null)
            return;

        if (!crossFade || !_bgmSource.isPlaying)
        {
            _bgmSource.clip = entry.Clip;
            _bgmSource.volume = targetVolume;
            _bgmSource.loop = true;
            _bgmSource.Play();
            return;
        }

        _bgmFadeTween = _bgmSource
            .DOFade(0f, bgmCrossFadeDuration * 0.5f)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _bgmSource.clip = entry.Clip;
                _bgmSource.volume = 0f;
                _bgmSource.loop = true;
                _bgmSource.Play();
                _bgmFadeTween = _bgmSource
                    .DOFade(targetVolume, bgmCrossFadeDuration * 0.5f)
                    .SetUpdate(true);
            });
    }

    public void StopBgm(float fadeOut = 0.5f)
    {
        if (_bgmSource == null)
            return;

        _currentBgmId = EBgmId.None;
        _bgmFadeTween?.Kill();

        if (fadeOut <= 0f)
        {
            _bgmSource.Stop();
            return;
        }

        _bgmFadeTween = _bgmSource
            .DOFade(0f, fadeOut)
            .SetUpdate(true)
            .OnComplete(() => _bgmSource.Stop());
    }

    public void SetBgmVolume(float normalized)
    {
        _bgmVolume = Mathf.Clamp01(normalized);
        SoundVolumeSettings.BgmVolume = _bgmVolume;
        ApplyVolumeSettings();
        RefreshBgmVolume();
    }

    public void SetSfxVolume(float normalized)
    {
        _sfxVolume = Mathf.Clamp01(normalized);
        SoundVolumeSettings.SfxVolume = _sfxVolume;
        ApplyVolumeSettings();
    }

    public float GetBgmVolume() => _bgmVolume;
    public float GetSfxVolume() => _sfxVolume;

    public void SaveVolumeSettings() => SoundVolumeSettings.Save();

    private void TryInitialize()
    {
        if (_isReady || soundDatabase == null)
            return;

        InitializeSources();
        LoadVolumeSettings();
        ApplyVolumeSettings();
        _isReady = true;
    }

    private void InitializeSources()
    {
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.playOnAwake = false;
        _bgmSource.loop = true;
        _bgmSource.spatialBlend = 0f;
        _bgmSource.ignoreListenerPause = true;

        if (bgmMixerGroup != null)
            _bgmSource.outputAudioMixerGroup = bgmMixerGroup;

        _sfxPool = new AudioSourcePool(transform, Mathf.Max(4, sfxPoolSize), sfxMixerGroup);
    }

    private void LoadVolumeSettings()
    {
        _bgmVolume = SoundVolumeSettings.BgmVolume;
        _sfxVolume = SoundVolumeSettings.SfxVolume;
    }

    private void ApplyVolumeSettings()
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat(BgmVolumeParam, LinearToDb(_bgmVolume));
            audioMixer.SetFloat(SfxVolumeParam, LinearToDb(_sfxVolume));
            return;
        }

        RefreshBgmVolume();
    }

    private void RefreshBgmVolume()
    {
        if (_bgmSource == null || _currentBgmId == EBgmId.None || soundDatabase == null)
            return;

        if (!soundDatabase.TryGetBgm(_currentBgmId, out BgmEntry entry))
            return;

        _bgmSource.volume = entry.Volume * _bgmVolume;
    }

    private bool CanPlaySfx(ESoundId id, float cooldownSeconds)
    {
        if (cooldownSeconds <= 0f)
            return true;

        return !_sfxCooldownUntil.TryGetValue(id, out float until) || Time.unscaledTime >= until;
    }

    private void MarkSfxPlayed(ESoundId id, float cooldownSeconds)
    {
        if (cooldownSeconds <= 0f)
            return;

        _sfxCooldownUntil[id] = Time.unscaledTime + cooldownSeconds;
    }

    private static float LinearToDb(float linear)
    {
        return linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus || !ReferenceEquals(Instance, this))
            return;

        SaveVolumeSettings();
    }

    private void OnApplicationQuit()
    {
        if (!ReferenceEquals(Instance, this))
            return;

        SaveVolumeSettings();
    }

    protected override void OnDestroy()
    {
        _bgmFadeTween?.Kill();
        base.OnDestroy();
    }
}
