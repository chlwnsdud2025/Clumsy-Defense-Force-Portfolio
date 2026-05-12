using UnityEngine;

public class GameSessionManager : MonoSingleton<GameSessionManager>
{
    private IEventBus _eventBus;

    private int _killCount;
    private int _gold;
    private float _playTime;
    private int _lastDisplayedSecond = -1;
    private bool _isPlaying;

    protected override void Awake()
    {
        base.Awake();

        _eventBus = ServiceLocator.Get<IEventBus>();
    }

    public void StartSession()
    {
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
        _killCount++;
        _eventBus.Publish(new KillCountEvent(_killCount));
    }

    public void AddGold(int amount)
    {
        _gold += amount;
        _eventBus.Publish(new GoldChangedEvent(_gold));
    }

    public void EndSession()
    {
        _isPlaying = false;
    }
}
