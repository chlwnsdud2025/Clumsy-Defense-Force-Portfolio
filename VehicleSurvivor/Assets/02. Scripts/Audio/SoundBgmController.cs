using UnityEngine;

[RequireComponent(typeof(SoundManager))]
public class SoundBgmController : MonoBehaviour
{
    private SoundManager _soundManager;
    private IEventBus _eventBus;

    private bool _isSubscribed;
    private bool _nightmareBgmPlayed;
    private bool _wasInRewardSelection;
    private bool _gameOverHandled;

    private void Awake()
    {
        _soundManager = GetComponent<SoundManager>();
    }

    private void OnEnable()
    {
        TrySubscribe();
        SyncTitleBgm();
    }

    private void OnDisable() => Unsubscribe();

    private void Update()
    {
        if (!GameManager.HasInstance)
            return;

        SyncTitleBgm();
        SyncRunSessionBgm();
    }

    private void TrySubscribe()
    {
        if (_isSubscribed)
            return;

        if (!ServiceLocator.TryGet(out _eventBus))
            return;

        _eventBus.Subscribe<WaveTimeChangedEvent>(OnWaveTimeChanged);
        _eventBus.Subscribe<BossAppearedEvent>(OnBossAppeared);
        _eventBus.Subscribe<BossDefeatedEvent>(OnBossDefeated);
        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed || _eventBus == null)
            return;

        _eventBus.Unsubscribe<WaveTimeChangedEvent>(OnWaveTimeChanged);
        _eventBus.Unsubscribe<BossAppearedEvent>(OnBossAppeared);
        _eventBus.Unsubscribe<BossDefeatedEvent>(OnBossDefeated);
        _isSubscribed = false;
    }

    private void SyncTitleBgm()
    {
        if (!GameManager.TryGetInstance(out GameManager gameManager))
            return;

        switch (gameManager.CurrentState)
        {
            case EGameFlowState.Title:
            case EGameFlowState.CharacterSelect:
                _soundManager.PlayBgm(EBgmId.BGM_Title);
                break;
        }
    }

    private void SyncRunSessionBgm()
    {
        if (!RunManager.TryGetInstance(out RunManager runManager))
            return;

        RunSession session = runManager.RunSession;
        if (session == null)
            return;

        if (session.State == ERunState.RewardSelection)
        {
            _wasInRewardSelection = true;
            _soundManager.PlayBgm(EBgmId.BGM_Reward);
            return;
        }

        if (session.State == ERunState.GameOver)
        {
            if (!_gameOverHandled)
            {
                _gameOverHandled = true;
                _soundManager.PlayBgm(EBgmId.BGM_GameOver);
                _soundManager.PlaySfx(ESoundId.SFX_GameOver);
            }

            return;
        }

        if (_wasInRewardSelection && session.State == ERunState.Playing)
        {
            _wasInRewardSelection = false;
            _soundManager.PlayBgm(EBgmId.BGM_InGame_Normal);
        }
    }

    private void OnWaveTimeChanged(WaveTimeChangedEvent e)
    {
        if (!e.IsNightmare || _nightmareBgmPlayed)
            return;

        _nightmareBgmPlayed = true;
        _soundManager.PlayBgm(EBgmId.BGM_InGame_Nightmare);
        _soundManager.PlaySfx(ESoundId.SFX_Wave_Nightmare);
    }

    private void OnBossAppeared(BossAppearedEvent _)
    {
        _soundManager.PlayBgm(EBgmId.BGM_Boss);
        _soundManager.PlaySfx(ESoundId.SFX_Boss_Appear);
    }

    private void OnBossDefeated(BossDefeatedEvent _)
    {
        _soundManager.PlaySfx(ESoundId.SFX_Boss_Defeat);
    }

    public void ResetRunState()
    {
        _nightmareBgmPlayed = false;
        _wasInRewardSelection = false;
        _gameOverHandled = false;
    }

    public void PlayInGameBgm() => _soundManager.PlayBgm(EBgmId.BGM_InGame_Normal);

    public void PlayHubBgm() => _soundManager.PlayBgm(EBgmId.BGM_Hub);
}
