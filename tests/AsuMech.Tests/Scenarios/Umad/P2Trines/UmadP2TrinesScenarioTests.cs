using System.Numerics;
using AsuMech.Scenarios.Umad.P2Trines;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Tests.NegativeRun;
using ActionId = AsuMech.Scenarios.Umad.UmadConstants.ActionId;
using Scenario = AsuMech.Scenarios.Umad.P2Trines.UmadP2TrinesScenario;

namespace AsuMech.Tests;

public class UmadP2TrinesScenarioTests
{
    [Test]
    public void StandingInTheWingsHalfKills()
        => Negative<Scenario>(RegenHealer)
            .Overrides<UmadP2TrinesStateOverrides>(o => o.CleaveWest = true)
            .TeleportAt(Scenario.CleaveAt - 0.4f, to: new Vector2(-5f, 0f))
            .ShouldKill(ActionId.WingsOfDestruction_West, RegenHealer);

    // The far tank waits 18-19.3y out; the edge of the arena outranges them. The party waits at
    // the trine north of the middle, far from that edge.
    [Test]
    public void ADpsFurtherOutThanTheFarTankTakesTheBuster()
        => Negative<Scenario>(PhysRangedDps)
            .Overrides<UmadP2TrinesStateOverrides>(o => o.FirstSet = [1, 2, 3])
            .TeleportAt(Scenario.NearFarSnapshotAt - 0.3f, to: new Vector2(19.9f, 0f))
            .ShouldKill(ActionId.WingsOfDestruction_Buster, PhysRangedDps);

}
