using System;
using System.Collections;
using System.Collections.Generic;

namespace AnoMech.Core.SimObjects;

// A container's owned children, plus at most one child per singleton type. Singletons are
// ordinary children that can also be looked up by type; setting a new one despawns and drops
// the one it replaces. Despawn runs in reverse insertion order, so a child added early outlives
// the ones added later that reference it.
public sealed class SimChildren : IEnumerable<ISimObject>
{
    private readonly List<ISimObject> all = new();
    private readonly Dictionary<Type, ISimObject> singletons = new();

    public T Add<T>(T child) where T : ISimObject
    {
        all.Add(child);
        return child;
    }

    public T SetSingleton<T>(T child) where T : class, ISimObject
    {
        if (singletons.TryGetValue(typeof(T), out var previous))
        {
            previous.Despawn();
            all.Remove(previous);
        }
        singletons[typeof(T)] = child;
        all.Add(child);
        return child;
    }

    public T? Singleton<T>() where T : class, ISimObject
        => singletons.TryGetValue(typeof(T), out var child) ? (T)child : null;

    public T GetOrAdd<T>(Func<T> create) where T : class, ISimObject
        => Singleton<T>() ?? SetSingleton(create());

    public void Update(float deltaSeconds)
    {
        foreach (var child in all)
            child.Tick(deltaSeconds);
        for (int i = all.Count - 1; i >= 0; i--)
        {
            var child = all[i];
            if (child.IsActive) continue;
            child.Despawn();
            all.RemoveAt(i);
            DropSingleton(child);
        }
    }

    public void Despawn()
    {
        for (int i = all.Count - 1; i >= 0; i--)
            all[i].Despawn();
        all.Clear();
        singletons.Clear();
    }

    private void DropSingleton(ISimObject child)
    {
        foreach (var (type, singleton) in singletons)
        {
            if (!ReferenceEquals(singleton, child)) continue;
            singletons.Remove(type);
            return;
        }
    }

    public IEnumerator<ISimObject> GetEnumerator() => all.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
