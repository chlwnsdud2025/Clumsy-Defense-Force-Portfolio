using System;

public interface IEventBus
{
    void Subscribe<T>(Action<T> callback, int priority = 0) where T : struct;

    void Unsubscribe<T>(Action<T> callback) where T : struct;

    void Publish<T>(T eventData) where T : struct;
}