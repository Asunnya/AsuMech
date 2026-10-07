using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

// Off by default, as when the user hasn't enabled the module.
internal sealed class FakeUserActions : IUserActions
{
    public bool Enabled { get; set; }
    public void OnSessionStart() { }
    public void OnScenarioStart() { }
    public void OnSessionEnd() { }
}
