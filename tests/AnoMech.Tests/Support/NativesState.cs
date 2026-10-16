using System.Reflection;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

// Everything Natives points at for one client: its fakes, and what its multiplayer layer installed
// over them. Natives is process-wide, so a test driving several clients swaps these.
internal sealed class NativesState
{
    private static readonly PropertyInfo[] Properties = typeof(Natives).GetProperties(BindingFlags.Public | BindingFlags.Static);
    private readonly object?[] values = new object?[Properties.Length];

    public void Capture()
    {
        for (var i = 0; i < Properties.Length; i++) values[i] = Properties[i].GetValue(null);
    }

    public void Restore()
    {
        for (var i = 0; i < Properties.Length; i++) Properties[i].SetValue(null, values[i]);
    }
}
