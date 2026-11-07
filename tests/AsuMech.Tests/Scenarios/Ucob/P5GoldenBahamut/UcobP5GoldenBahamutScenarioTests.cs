using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Ucob.P5GoldenBahamut;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Morn Afah 1 is cast at 35.14 and snapshots at 40.84; Akh Morn 1 at 43.43 (snapshot 47.13), Akh
// Morn 2 at 75.03 (snapshot 78.73, alone under an invuln). The first Exaflare set is pinned to roll
// north with lanes fired as pairs (4, -12), (-20, 12), (-4, 20): lane 4 first erupts at (4, 20) at 61.89.
public class UcobP5GoldenBahamutScenarioTests
{
    private static readonly float[] LaneOrder = [4f, -12f, -20f, 12f, -4f, 20f];

    private static NegativeRun<UcobP5GoldenBahamutScenario> GoldenBahamut(PartyRole player)
        => Negative<UcobP5GoldenBahamutScenario>(player)
            .Overrides<UcobP5GoldenBahamutStateOverrides>(o =>
            {
                o.ExaflareDirections[0] = Direction.N;
                o.ExaflareLaneOrders = [LaneOrder, LaneOrder, LaneOrder, LaneOrder];
                o.MornAfahTargets = [RegenHealer, RegenHealer, RegenHealer, RegenHealer, RegenHealer];
            });

    [Test]
    public void StandingOutOfMornAfahLeavesTheStackTooLight()
        => GoldenBahamut(MeleeDpsA)
            .TeleportAt(37f, to: new(12, 0))
            .ShouldKill(ActionId.MornAfah, AllBut(MeleeDpsA));

    [Test]
    public void TheMarkedPlayerRunningOffTakesMornAfahAlone()
        => GoldenBahamut(RegenHealer)
            .TeleportAt(37f, to: new(12, 0))
            .ShouldKill(ActionId.MornAfah, RegenHealer)
            .ShouldKillNoOneElse();

    [Test]
    public void ASharedAkhMornTakenAloneKills()
        => GoldenBahamut(MainTank)
            .WithMitigationChecks()
            .TeleportAt(44f, to: new(10, -3))
            .ShouldKill(ActionId.AkhMorn, MainTank);

    [Test]
    public void TheLoneAkhMornWithoutAnInvulnKills()
        => GoldenBahamut(MainTank)
            .WithMitigationChecks()
            .TeleportAt(76f, to: new(0, -3))
            .ShouldKill(ActionId.AkhMorn, MainTank);

    [Test]
    public void ANonTankInAkhMornDies()
        => GoldenBahamut(CasterDps)
            .TeleportAt(44f, to: new(0, -3))
            .ShouldKill(ActionId.AkhMorn, CasterDps);

    [Test]
    public void StandingOnAnExaflareLaneKills()
        => GoldenBahamut(PhysRangedDps)
            .TeleportAt(58f, to: new(4, 19))
            .ShouldKill(ActionId.ExaflareFirst, PhysRangedDps);

    [Test]
    public void TheLoneAkhMornUnderAnInvulnIsSurvived()
    {
        var run = ScenarioRun.Execute(typeof(UcobP5GoldenBahamutScenario), 0, 5, new ScenarioRunOptions
        {
            PlayerRole = MainTank,
            UserActionsEnabled = true,
            Takeovers = [new Takeover(76f, new Vector2(0, -3))],
            Probe = p =>
            {
                if (p.Crossed(77f)) p.Member(MainTank)?.AddStatus(HallowedGround, 10f);
            },
            StopAt = 90f,
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void BotsClearWithTheOffTankInvulningFirst([Range(0, 4)] int seed)
    {
        var run = ScenarioRun.Execute(typeof(UcobP5GoldenBahamutScenario), 0, seed, new ScenarioRunOptions
        {
            Overrides = o => ((UcobP5GoldenBahamutStateOverrides)o).FirstInvulnTank = OffTank,
        });
        Assert.That(run.Passed, run.ToString);
    }

    private const ushort HallowedGround = 82;
}
