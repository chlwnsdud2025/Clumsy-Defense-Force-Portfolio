using System;
using System.Collections.Generic;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    public static void Register<T>(T service) where T : class
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        _services[typeof(T)] = service;
    }

    public static bool TryGet<T>(out T service) where T : class
    {
        if (_services.TryGetValue(typeof(T), out object registeredService))
        {
            service = registeredService as T;
            return service != null;
        }

        service = null;
        return false;
    }

    public static T Get<T>() where T : class
    {
        if (TryGet(out T service))
            return service;

        throw new InvalidOperationException($"[ServiceLocator] {typeof(T).Name} 서비스가 등록되지 않았습니다.");
    }

    public static bool IsRegistered<T>() where T : class
    {
        return _services.ContainsKey(typeof(T));
    }

    public static void Unregister<T>() where T : class
    {
        _services.Remove(typeof(T));
    }

    public static void Clear()
    {
        _services.Clear();
    }
}