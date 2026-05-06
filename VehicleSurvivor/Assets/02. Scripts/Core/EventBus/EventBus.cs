using System;
using System.Collections.Generic;

/// <summary>
/// 우선순위 기반 이벤트 버스. 숫자가 클수록 먼저 호출됩니다. 같으면 구독한 순서대로입니다.
/// </summary>
public class EventBus : IEventBus
{
    private sealed class HandlerEntry
    {
        public int Priority;
        public long Order;
        public Delegate Callback;
    }

    private readonly Dictionary<Type, List<HandlerEntry>> _handlers = new Dictionary<Type, List<HandlerEntry>>();
    private long _orderCounter;

    public void Subscribe<T>(Action<T> callback, int priority = 0) where T : struct
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        Type eventType = typeof(T);

        if (!_handlers.TryGetValue(eventType, out List<HandlerEntry> list))
        {
            list = new List<HandlerEntry>();
            _handlers[eventType] = list;
        }

        list.Add(new HandlerEntry
        {
            Priority = priority,
            Order = ++_orderCounter,
            Callback = callback
        });

        list.Sort(CompareHandlers);
    }

    /// <summary>
    /// 동일한 delegate 참조로 등록된 모든 핸들러를 제거합니다.
    /// </summary>
    public void Unsubscribe<T>(Action<T> callback) where T : struct
    {
        if (callback == null)
            return;

        Type eventType = typeof(T);

        if (!_handlers.TryGetValue(eventType, out List<HandlerEntry> list))
            return;

        list.RemoveAll(e => e.Callback.Equals(callback));

        if (list.Count == 0)
            _handlers.Remove(eventType);
    }

    public void Publish<T>(T eventData) where T : struct
    {
        Type eventType = typeof(T);

        if (!_handlers.TryGetValue(eventType, out List<HandlerEntry> list) || list.Count == 0)
            return;

        int count = list.Count;
        HandlerEntry[] snapshot = new HandlerEntry[count];
        for (int i = 0; i < count; i++)
            snapshot[i] = list[i];

        for (int i = 0; i < count; i++)
        {
            if (snapshot[i].Callback is Action<T> action)
                action.Invoke(eventData);
        }
    }

    private static int CompareHandlers(HandlerEntry a, HandlerEntry b)
    {
        int priorityCompare = b.Priority.CompareTo(a.Priority);
        if (priorityCompare != 0)
            return priorityCompare;

        return a.Order.CompareTo(b.Order);
    }
}