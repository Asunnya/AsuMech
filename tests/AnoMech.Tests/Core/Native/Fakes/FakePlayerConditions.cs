using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

internal sealed class FakePlayerConditions : IPlayerConditions
{
    public bool StatusAffliction { get; private set; }
    public bool ControlAffliction { get; private set; }
    public void EndClientLoad() { }

    public void SetAfflictions(bool status, bool control)
    {
        StatusAffliction = status;
        ControlAffliction = control;
    }
}
