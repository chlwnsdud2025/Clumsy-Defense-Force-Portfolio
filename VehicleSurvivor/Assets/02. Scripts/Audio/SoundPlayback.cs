public static class SoundPlayback
{
    public static void PlaySfx(ESoundId id, float volumeScale = 1f)
    {
        if (SoundManager.TryGetInstance(out SoundManager manager))
            manager.PlaySfx(id, volumeScale);
    }

    public static void PlayBgm(EBgmId id, bool crossFade = true)
    {
        if (SoundManager.TryGetInstance(out SoundManager manager))
            manager.PlayBgm(id, crossFade);
    }
}
