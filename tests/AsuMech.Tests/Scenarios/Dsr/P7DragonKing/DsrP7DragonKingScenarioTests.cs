using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Dsr.P7DragonKing;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Dsr.DsrConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

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

    // The main tank holds enmity for the first Exaflare; from behind him the spots flip to his south.
    [Test]
    public void TheTopEnmityTankTurnsTheExaflaresWithTheDragonKing()
        => Negative<DsrP7DragonKingScenario>(MainTank)
            .Overrides<DsrP7DragonKingStateOverrides>(Pinned)
            .TeleportAt(38f, to: new Vector2(0f, 6f))
            .TeleportAt(46.5f, to: new Vector2(6.93f, 4f))
            .ShouldKill(ActionId.ExaflaresEdgeFirst, MainTank);

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
            .WithMitigationChecks()
            .TeleportAt(145f, to: new Vector2(0f, 9f))
            .ShouldKill(ActionId.AkhMornsEdgeTanks, MainTank);

    // His swings draw the Akh Morn hits around his facing, so the tank taking enmity at the cast
    // (south, in the rear tower) mustn't turn him until the last hit.
    [Test]
    public void TheDragonKingHoldsHisFacingUntilAkhMornResolves()
        => Negative<DsrP7DragonKingScenario>(PhysRangedDps)
            .Overrides<DsrP7DragonKingStateOverrides>(Pinned)
            .At(DsrP7DragonKingScenario.AkhMornLastHit(1) - 0.1f, p =>
            {
                var thordan = p.World.Children.OfType<SimEnemy>().Single(e => e.BNpcBaseId == BNpcBaseId.DragonKingThordan);
                Assert.That(MathF.Abs(MathF.IEEERemainder(thordan.Rotation - MathF.PI, MathF.Tau)), Is.LessThan(0.05f));
            })
            .ShouldKillNoOneElse();

    private static readonly Vector2 SecondAkhMornSideTower = new(7.8f, -4.5f);

    [Test]
    public void KitchenSinkingTheSideTowerWithoutMitigationKills()
        => Negative<DsrP7DragonKingScenario>(OffTank)
            .Overrides<DsrP7DragonKingStateOverrides>(Pinned)
            .WithMitigationChecks()
            .TeleportAt(141f, to: SecondAkhMornSideTower)
            .ShouldKill(ActionId.AkhMornsEdgeParty, OffTank);

    // Rampart and Damnation; standing on at the front right, the next death is the first Gigaflare.
    [Test]
    public void KitchenSinkingTheSideTowerOnMitigationLives()
        => Negative<DsrP7DragonKingScenario>(OffTank)
            .Overrides<DsrP7DragonKingStateOverrides>(Pinned)
            .WithMitigationChecks()
            .TeleportAt(141f, to: SecondAkhMornSideTower)
            .At(146f, p =>
            {
                p.Member(OffTank)!.AddStatus(1191, 20f);
                p.Member(OffTank)!.AddStatus(3832, 20f);
            })
            .TeleportAt(DsrP7DragonKingScenario.AkhMornLastHit(1) + 0.3f, to: new Vector2(7.8f, -7.8f))
            .ShouldKill(ActionId.GigaflaresEdgeFirst, OffTank);

    [Test]
    public void TheBotsClearWithTheOffTankInvulningFirst()
        => Negative<DsrP7DragonKingScenario>(PhysRangedDps)
            .Overrides<DsrP7DragonKingStateOverrides>(o => { Pinned(o); o.FirstInvulnTank = OffTank; })
            .ShouldKillNoOneElse();

    [Test]
    public void StandingInTheMiddleForGigaflareKills()
        => Negative<DsrP7DragonKingScenario>(CasterDps)
            .Overrides<DsrP7DragonKingStateOverrides>(Pinned)
            .TeleportAt(94.3f, to: Vector2.Zero)
            .ShouldKill(ActionId.GigaflaresEdgeFirst, CasterDps);
}
