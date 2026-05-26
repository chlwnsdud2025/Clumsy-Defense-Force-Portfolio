using System;
using UnityEngine;

[Serializable]
public class SfxEntry
{
    [SerializeField] private ESoundId id;
    [SerializeField] private AudioClip[] variations;
    [SerializeField] private float volume = 1f;
    [SerializeField] private float pitchMin = 0.95f;
    [SerializeField] private float pitchMax = 1.05f;
    [SerializeField] private float cooldownSeconds;
    [SerializeField] private ESfxPriority priority = ESfxPriority.Normal;

    public ESoundId Id => id;
    public float Volume => volume;
    public float PitchMin => pitchMin;
    public float PitchMax => pitchMax;
    public float CooldownSeconds => cooldownSeconds;
    public ESfxPriority Priority => priority;

    public bool TryGetClip(out AudioClip clip)
    {
        clip = null;
        if (variations == null || variations.Length == 0)
            return false;

        if (variations.Length == 1)
        {
            clip = variations[0];
            return clip != null;
        }

        int start = UnityEngine.Random.Range(0, variations.Length);
        for (int i = 0; i < variations.Length; i++)
        {
            AudioClip candidate = variations[(start + i) % variations.Length];
            if (candidate != null)
            {
                clip = candidate;
                return true;
            }
        }

        return false;
    }
}

[Serializable]
public class BgmEntry
{
    [SerializeField] private EBgmId id;
    [SerializeField] private AudioClip clip;
    [SerializeField] private float volume = 1f;

    public EBgmId Id => id;
    public AudioClip Clip => clip;
    public float Volume => volume;
}
