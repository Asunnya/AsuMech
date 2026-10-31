using System.Linq;
using System.Numerics;
using AnoMech.Scenarios.Dsr.P7DragonKing;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Dsr.DsrConstants;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
// Pinned to the clear: Flames on the first three Edges, Ice on the second Exaflare.
public class DsrP7DragonKingScenarioTests
{
    private static void Pinned(DsrP7DragonKingStateOverrides o)
    {
        var pinned = DsrP7DragonKingStateTests.Pinned();
        o.ExaflareHeadings = pinned.ExaflareHeadings;
        o.Elements = pinned.Elements;
        o.GigaflareOrder = pinned.GigaflareOrder;
    }

    private static void IceFirst(DsrP7DragonKingStateOverrides o)
    {
        Pinned(o);
        o.Elements = [AscalonElement.Ice, .. o.Elements!.Skip(1)];
    }

    [Test]
    public void StandingOnAnExaflareStartKills()
        => Negative<DsrP7DragonKingScenario>(MeleeDpsB)
            .Overrides<DsrP7DragonKingStateOverrides>(Pinned)
            .TeleportAt(46.5f, to: new Vector2(-6.93f, -4f))
            .ShouldKill(ActionId.ExaflaresEdgeFirst, MeleeDpsB);

    [Test]
    public void StayingNearTheDragonKingForFlamesKills()
        => Negative<DsrP7DragonKingScenario>(CasterDps)
            .Overrides<DsrP7DragonKingStateOverrides>(Pinned)
            .TeleportAt(46.5f, to: new Vector2(2f, 0f))
            .ShouldKill(ActionId.FlamesOfAscalon, CasterDps);

    [Test]
    public void StayingAwayForIceKills()
        => Negative<DsrP7DragonKingScenario>(CasterDps)
            .Overrides<DsrP7DragonKingStateOverrides>(IceFirst)
            .TeleportAt(46.5f, to: new Vector2(0f, -15f))
            .ShouldKill(ActionId.IceOfAscalon, CasterDps);

    [Test]
    public void TheBotsClearThePinnedPullWithIceOnTheFirstExaflare()
        => Negative<DsrP7DragonKingScenario>(PhysRangedDps)
            .Overrides<DsrP7DragonKingStateOverrides>(IceFirst)
            .ShouldKillNoOneElse();

    [Test]
    public void BaitingTrinityTwiceInARowKills()
        => Negative<DsrP7DragonKingScenario>(MeleeDpsA)
            .Overrides<DsrP7DragonKingStateOverrides>(Pinned)
            .TeleportAt(58.5f, to: new Vector2(0f, 0.5f))
            .ShouldKill(ActionId.TrinityNearest, MeleeDpsA);

    [Test]
    public void TheTanksTradeTrinityElementsEachPair()
        => Negative<DsrP7DragonKingScenario>(PhysRangedDps)
            .Overrides<DsrP7DragonKingStateOverrides>(Pinned)
            .At(60f, p =>
            {
                Assert.That(p.Member(MainTank)!.HasStatus(StatusId.DarkResistanceDown));
                Assert.That(p.Member(OffTank)!.HasStatus(StatusId.TrinityLightResistanceDown));
                Assert.That(p.Member(MeleeDpsA)!.HasStatus(StatusId.PhysicalVulnerabilityUp));
                Assert.That(p.Member(MeleeDpsB)!.HasStatus(StatusId.PhysicalVulnerabilityUp));
                Assert.That(p.Member(PhysRangedDps)!.HasStatus(StatusId.PhysicalVulnerabilityUp), Is.False);
            })
            .At(84.5f, p =>
            {
                Assert.That(p.Member(OffTank)!.HasStatus(StatusId.DarkResistanceDown));
                Assert.That(p.Member(MainTank)!.HasStatus(StatusId.TrinityLightResistanceDown));
            })
            .ShouldKillNoOneElse();

    [Test]
    public void HoldingTheTankAkhMornWithoutAnInvulnKills()
        => Negative<DsrP7DragonKingScenario>(MainTank)
            .Overrides<DsrP7DragonKingStateOverrides>(Pinned)
            .TeleportAt(145f, to: new Vector2(0f, 9f))
            .ShouldKill(ActionId.AkhMornsEdgeTanks, MainTank);

    [Test]
    public void StandingInTheMiddleForGigaflareKills()
        => Negative<DsrP7DragonKingScenario>(CasterDps)
            .Overrides<DsrP7DragonKingStateOverrides>(Pinned)
            .TeleportAt(94.3f, to: Vector2.Zero)
            .ShouldKill(ActionId.GigaflaresEdgeFirst, CasterDps);
}
