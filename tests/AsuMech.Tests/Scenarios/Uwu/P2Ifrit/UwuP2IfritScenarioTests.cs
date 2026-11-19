using System.Linq;
using System.Numerics;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Uwu.P2Ifrit;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Uwu.UwuConstants;

namespace AsuMech.Tests;

public class UwuP2IfritScenarioTests
{
    [Test]
    public void RadiantPlumesShowTheirOmensWhileTheirCastersAreStillArriving()
    {
        var plumesCasting = 0;
        ScenarioRun.Execute(typeof(UwuP2IfritScenario), 0, 1234, new ScenarioRunOptions
        {
            PacketSpawnFrames = (2, 5),
            StopAt = 7f,
            WriteArtifactsOnFailure = false,
            Probe = probe =>
            {
                if (!probe.Crossed(6f)) return;
                plumesCasting = probe.World.Children.OfType<SimEnemy>()
                    .Count(e => e.IsCasting && e.CastActionId == ActionId.RadiantPlumePuddle);
            },
        });

        Assert.That(plumesCasting, Is.GreaterThan(0));
    }

    // The bots take it under knockback immunity; the player, off the AI, takes the push.
    [Test]
    public void VulcanBurstThrowsWhoeverHasNoKnockbackImmunityFifteenYalms()
    {
        Vector3 playerBefore = default, playerAfter = default, botBefore = default, botAfter = default;
        ScenarioRun.Execute(typeof(UwuP2IfritScenario), 0, 1234, new ScenarioRunOptions
        {
            PlayerRole = CasterDps,
            StopAt = 25.5f,
            WriteArtifactsOnFailure = false,
            Takeovers = [new Takeover(22f, Vector2.Zero)],
            Probe = probe =>
            {
                if (probe.Crossed(23.5f)) (playerBefore, botBefore) = (probe.Member(CasterDps)!.Position, probe.Member(PhysRangedDps)!.Position);
                if (probe.Crossed(25.4f)) (playerAfter, botAfter) = (probe.Member(CasterDps)!.Position, probe.Member(PhysRangedDps)!.Position);
            },
        });
        Assert.That(Vector3.Distance(playerAfter, playerBefore), Is.EqualTo(15f).Within(0.5f));
        Assert.That(Vector3.Distance(botAfter, botBefore), Is.LessThan(1f));
    }
}
