using System.Reflection;
using AsuMech.Multiplayer;
using NUnit.Framework.Interfaces;

[assembly: AsuMech.Tests.NoSessionOutlivesItsTest]

namespace AsuMech.Tests;

// Plugin.Multiplayer is process-wide, and a host's session left in it turns per-role settings on
// (PerRole.SeatsActive) for whichever test runs next. Each test that leaves one fails, and the
// next starts without it.
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class NoSessionOutlivesItsTestAttribute : Attribute, ITestAction
{
    public ActionTargets Targets => ActionTargets.Test;

    public void BeforeTest(ITest test) { }

    public void AfterTest(ITest test)
    {
        if (Leftover() is not { } manager) return;
        DalamudServices.Install(nameof(Plugin.Multiplayer), null);
        Assert.Fail($"the test left Plugin.Multiplayer in a session ({(manager.IsHost ? "host" : "peer")}, {manager.SessionCode})");
    }

    private static MultiplayerManager? Leftover()
        => typeof(Plugin).GetProperty(nameof(Plugin.Multiplayer), BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)
            is MultiplayerManager { SessionCode: not null } manager ? manager : null;
}
