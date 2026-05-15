using UnityEngine;

public class RunManager : MonoSceneSingleton<RunManager>
{
    private IEventBus _eventBus;
    private RunContext _runContext;

    private int _killCount;
    private int _gold;
    private float _playTime;
    private int _lastDisplayedSecond = -1;
    private bool _isPlaying;

    public RunContext RunContext => _runContext;


    public void StartRun(CharacterDefinitionSO selectedCharacter)
    {
        if (selectedCharacter == null)
        {
            Debug.LogError("[RunManager] 선택된 캐릭터가 없어 Run을 시작할 수 없습니다.");
            return;
        }

        if (_eventBus == null)
        {
            _eventBus = ServiceLocator.Get<IEventBus>();
        }

        _runContext = new RunContext(selectedCharacter);

        _killCount = 0;
        _gold = 0;
        _playTime = 0f;
        _lastDisplayedSecond = -1;
        _isPlaying = true;

        _eventBus.Publish(new HpChangedEvent(_runContext.CurrentHp, _runContext.MaxHp));
        _eventBus.Publish(new KillCountEvent(_killCount));
        _eventBus.Publish(new GoldChangedEvent(_gold));
        _eventBus.Publish(new TimeChangeEvent(_playTime));
    }

    private void Update()
    {
        if (!_isPlaying) return;

        _playTime += Time.deltaTime;

        int currentSecond = Mathf.FloorToInt(_playTime);
        if (currentSecond != _lastDisplayedSecond)
        {
            _lastDisplayedSecond = currentSecond;
            _eventBus.Publish(new TimeChangeEvent(_playTime));
        }
    }

    public void AddKill()
    {
        if (!_isPlaying) return;

        if (_eventBus == null)
        {
            _eventBus = ServiceLocator.Get<IEventBus>();

        }

        _killCount++;
        _eventBus.Publish(new KillCountEvent(_killCount));
    }

    public void AddGold(int amount)
    {
        if (!_isPlaying) return;

        if (_eventBus == null)
        {
            _eventBus = ServiceLocator.Get<IEventBus>();
        }

        _gold += amount;
        _eventBus.Publish(new GoldChangedEvent(_gold));
    }

    public void EndRun()
    {
        _isPlaying = false;
        _runContext = null;

    }
}
