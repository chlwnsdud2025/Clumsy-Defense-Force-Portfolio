using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioSourcePool
{
    private sealed class PooledSource
    {
        public readonly AudioSource Source;
        public ESfxPriority Priority;
        public float StartedAt;

        public PooledSource(AudioSource source)
        {
            Source = source;
        }

        public bool IsPlaying => Source != null && Source.isPlaying;
    }

    private readonly List<PooledSource> _sources = new();

    public AudioSourcePool(Transform parent, int poolSize, AudioMixerGroup outputGroup = null)
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject child = new GameObject($"SfxSource_{i}");
            child.transform.SetParent(parent, false);

            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            if (outputGroup != null)
                source.outputAudioMixerGroup = outputGroup;

            _sources.Add(new PooledSource(source));
        }
    }

    public bool TryPlay(
        AudioClip clip,
        float volume,
        float pitch,
        ESfxPriority priority,
        out AudioSource playedSource)
    {
        playedSource = null;
        if (clip == null)
            return false;

        PooledSource target = AcquireSource(priority);
        if (target == null)
            return false;

        AudioSource source = target.Source;
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = 0f;
        source.loop = false;
        source.Play();

        target.Priority = priority;
        target.StartedAt = Time.unscaledTime;
        playedSource = source;
        return true;
    }

    private PooledSource AcquireSource(ESfxPriority priority)
    {
        PooledSource idle = null;
        PooledSource oldestLow = null;
        PooledSource oldestNormal = null;
        float oldestLowTime = float.MaxValue;
        float oldestNormalTime = float.MaxValue;

        for (int i = 0; i < _sources.Count; i++)
        {
            PooledSource candidate = _sources[i];
            if (!candidate.IsPlaying)
            {
                idle = candidate;
                break;
            }

            if (candidate.Priority == ESfxPriority.Low && candidate.StartedAt < oldestLowTime)
            {
                oldestLow = candidate;
                oldestLowTime = candidate.StartedAt;
            }
            else if (candidate.Priority == ESfxPriority.Normal && candidate.StartedAt < oldestNormalTime)
            {
                oldestNormal = candidate;
                oldestNormalTime = candidate.StartedAt;
            }
        }

        if (idle != null)
            return idle;

        if (priority == ESfxPriority.Low)
            return null;

        if (oldestLow != null)
            return Steal(oldestLow);

        if (priority == ESfxPriority.Normal)
            return oldestNormal != null ? Steal(oldestNormal) : null;

        if (oldestNormal != null)
            return Steal(oldestNormal);

        return Steal(_sources[0]);
    }

    private static PooledSource Steal(PooledSource source)
    {
        source.Source.Stop();
        return source;
    }
}
