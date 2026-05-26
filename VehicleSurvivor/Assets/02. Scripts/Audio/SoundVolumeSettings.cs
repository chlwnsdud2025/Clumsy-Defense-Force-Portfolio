using UnityEngine;

public static class SoundVolumeSettings
{
    public const string BgmVolumeKey = "Audio.BgmVolume";
    public const string SfxVolumeKey = "Audio.SfxVolume";

    public const float DefaultVolume = 0.8f;

    public static float BgmVolume
    {
        get => PlayerPrefs.GetFloat(BgmVolumeKey, DefaultVolume);
        set => PlayerPrefs.SetFloat(BgmVolumeKey, Mathf.Clamp01(value));
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxVolumeKey, DefaultVolume);
        set => PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
    }

    public static void Save() => PlayerPrefs.Save();
}
