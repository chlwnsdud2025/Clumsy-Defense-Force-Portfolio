using System.Collections.Generic;
using UnityEngine;

public readonly struct BlockableEntry
{
    public BlockableEntry(Transform transform, float blockRadius)
    {
        Transform = transform;
        BlockRadius = blockRadius;
    }

    public Transform Transform { get; }
    public float BlockRadius { get; }
}

public static class BlockableRegistry
{
    private static readonly List<BlockableEntry> Registered = new();

    public static void Register(Transform transform, float blockRadius)
    {
        if (transform == null)
            return;

        for (int i = 0; i < Registered.Count; i++)
        {
            if (Registered[i].Transform == transform)
            {
                Registered[i] = new BlockableEntry(transform, blockRadius);
                return;
            }
        }

        Registered.Add(new BlockableEntry(transform, blockRadius));
    }

    public static void Unregister(Transform transform)
    {
        if (transform == null)
            return;

        for (int i = Registered.Count - 1; i >= 0; i--)
        {
            if (Registered[i].Transform == transform)
                Registered.RemoveAt(i);
        }
    }

    public static IReadOnlyList<BlockableEntry> Entries => Registered;
}
