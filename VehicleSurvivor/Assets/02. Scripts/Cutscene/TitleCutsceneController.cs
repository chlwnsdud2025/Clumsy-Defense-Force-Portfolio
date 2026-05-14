using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

public class TitleCutsceneController : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private PlayableDirector titleToCharacterSelectTimeline;

    [Header("Cameras")]
    [SerializeField] private CinemachineCamera titleCamera;
    [SerializeField] private CinemachineCamera characterSelectCamera;

    [Header("Camera Priority")]
    [SerializeField] private int titleInactivePriority = 0;
    [SerializeField] private int characterSelectActivePriority = 20;

    private bool _isPlaying;
    private Action _onComplete;

    public void Play(Action onComplete)
    {
        if (_isPlaying) return;

        if (titleToCharacterSelectTimeline == null)
        {
            Debug.LogError("[TitleCutsceneController] PlayableDirector가 할당되지 않았습니다.");
            return;
        }

        if (titleToCharacterSelectTimeline.playableAsset == null)
        {
            Debug.LogError("[TitleCutsceneController] Timeline Asset이 연결되지 않았습니다.");
            return;
        }

        _isPlaying = true;
        _onComplete = onComplete;

        PrepareFinalCamera();

        titleToCharacterSelectTimeline.stopped -= OnTimelineStopped;
        titleToCharacterSelectTimeline.stopped += OnTimelineStopped;

        titleToCharacterSelectTimeline.time = 0;
        titleToCharacterSelectTimeline.Evaluate();
        titleToCharacterSelectTimeline.Play();
    }

    private void PrepareFinalCamera()
    {
        if (titleCamera != null)
        {
            titleCamera.Priority = titleInactivePriority;

        }

        if (characterSelectCamera != null)
        {
            characterSelectCamera.Priority = characterSelectActivePriority;

        }
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        if (director != titleToCharacterSelectTimeline) return;

        titleToCharacterSelectTimeline.stopped -= OnTimelineStopped;

        _isPlaying = false;

        _onComplete?.Invoke();
        _onComplete = null;
    }

    private void OnDestroy()
    {
        if (titleToCharacterSelectTimeline != null)
        {
            titleToCharacterSelectTimeline.stopped -= OnTimelineStopped;

        }
    }
}