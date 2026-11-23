using AsuMech.Core.Native.Interfaces;
using AsuMech.Scenarios.M12s.Act3;
using AsuMech.Scenarios.M12s.Replication1;

namespace AsuMech.Tests;

public class M12sZoneTests
{
    [TestCase(typeof(M12sAct3Scenario), "4,4,4,4")]
    [TestCase(typeof(M12sReplication1Scenario), "16384,1,4,4")]
    public void EveryRunStartsOnTheFightsRestingArena(System.Type scenario, string firstSlots)
    {
        var run = ScenarioRun.Execute(scenario, 0, 1);
        Assert.That(run.Passed, run.ToString);
        var snapshot = ((FakeBattleCharas)Natives.BattleCharas).Timeline.Rows.First(r => r["kind"] == "map_effect_snapshot");
        Assert.That(snapshot["detail"], Does.StartWith($"states={firstSlots},"));
    }
}
