using UnityEngine;

public static class ComponentExtensions
{
    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
        if (gameObject == null)
            return null;

        T component = gameObject.GetComponent<T>();
        if (component != null)
            return component;

        return gameObject.AddComponent<T>();
    }

    public static T GetOrAddComponent<T>(this Component component) where T : Component
    {
        if (component == null)
            return null;

        return component.gameObject.GetOrAddComponent<T>();
    }
}
