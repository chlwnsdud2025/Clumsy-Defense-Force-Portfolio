using UnityEngine;

public class RunManager : MonoSingleton<RunManager>
{
    private IEventBus _eventBus;

    private int _killCount;
    private int _gold;
    private float _playTime;
    private int _lastDisplayedSecond = -1;
    private bool _isPlaying;



    public void StartRun()
    {
        if (_eventBus == null)
            _eventBus = ServiceLocator.Get<IEventBus>();
        _killCount = 0;
        _gold = 0;
        _playTime = 0f;
        _lastDisplayedSecond = -1;
        _isPlaying = true;

        _eventBus.Publish(new KillCountEvent(_killCount));
        _eventBus.Publish(new GoldChangedEvent(_gold));
        _eventBus.Publish(new TimeChangeEvent(_playTime));
    }

    private void Update()
    {
        if (!_isPlaying)
            return;

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
            _eventBus = ServiceLocator.Get<IEventBus>();

        _killCount++;
        _eventBus.Publish(new KillCountEvent(_killCount));
    }

    public void AddGold(int amount)
    {
        if (!_isPlaying) return;

        if (_eventBus == null)
            _eventBus = ServiceLocator.Get<IEventBus>();

        _gold += amount;
        _eventBus.Publish(new GoldChangedEvent(_gold));
    }

    public void EndRun()
    {
        _isPlaying = false;
    }
}
