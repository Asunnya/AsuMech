using System.Numerics;
using AnoMech.Scenarios.Dsr.P5Wrath;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Dsr.DsrConstants;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class DsrP5WrathScenarioTests
{
    private const float W = DsrP5WrathScenario.Wrath;

    // Dragon east, warrior east and mage west. The plain wall is south: M1 M2 R C from east to west at r15,
    // 23 degrees apart; the divebomb behind the mage, the tethers and defamation on the north wall.
    private static void Pinned(DsrP5WrathStateOverrides o)
    {
        o.DragonBearing = 90f;
        o.MageOnDragonSide = false;
        o.Tethered = [MainTank, OffTank];
        o.Defamation = RegenHealer;
        o.Divebomb = ShieldHealer;
        o.Lightning = [MeleeDpsA, MeleeDpsB];
        o.LiquidHeaven = PhysRangedDps;
        o.Eruptions = MainTank;
    }

    [Test]
    public void StayingOnYourTwisterKills()
        => Negative<DsrP5WrathScenario>(CasterDps)
            .Overrides<DsrP5WrathStateOverrides>(Pinned)
            .FreezeAt(W + 15f)
            .ShouldKill(ActionId.Twister, CasterDps);

    [Test]
    public void StandingInTwistingDiveKills()
        => Negative<DsrP5WrathScenario>(CasterDps)
            .Overrides<DsrP5WrathStateOverrides>(Pinned)
            .TeleportAt(W + 14f, to: Vector2.Zero)
            .ShouldKill(ActionId.TwistingDive, CasterDps);

    // As the cones land: four on the south wall, the divebomb behind the mage.
    [Test]
    public void BotsTakeTheirConesOnThePlainWall()
        => Negative<DsrP5WrathScenario>(CasterDps)
            .Overrides<DsrP5WrathStateOverrides>(Pinned)
            .At(W + 22.1f, p =>
            {
                foreach (var role in new[] { MeleeDpsA, MeleeDpsB, PhysRangedDps })
                {
                    var at = p.Member(role)!.Position;
                    Assert.That(at.Z, Is.GreaterThan(10f), role.ToString());
                    Assert.That(new Vector2(at.X, at.Z).Length(), Is.EqualTo(15f).Within(1f), role.ToString());
                }
                var divebomb = p.Member(ShieldHealer)!.Position;
                Assert.That(divebomb.X, Is.LessThan(-17f));
            })
            .TeleportAt(W + 28.5f, to: new Vector2(-15f, 0f))
            .ShouldKill(ActionId.CauterizeDarkscale, CasterDps);

    [Test]
    public void SharingAProteanKillsBoth()
        => Negative<DsrP5WrathScenario>(CasterDps)
            .Overrides<DsrP5WrathStateOverrides>(Pinned)
            .TeleportAt(W + 21.5f, to: new Vector2(-2.5f, 14.7f))
            .ShouldKill(ActionId.AscalonsMercyRevealedCone, CasterDps, PhysRangedDps);

    [Test]
    public void StandingStillWhileBaitingEruptionsKills()
        => Negative<DsrP5WrathScenario>(MainTank)
            .Overrides<DsrP5WrathStateOverrides>(Pinned)
            .FreezeAt(W + 22.1f)
            .ShouldKill(ActionId.AltarFlareEruption, MainTank);

    [Test]
    public void StackingTheLightningKillsBoth()
        => Negative<DsrP5WrathScenario>(MeleeDpsA)
            .Overrides<DsrP5WrathStateOverrides>(Pinned)
            .TeleportAt(W + 29.25f, to: new Vector2(13.5f, -3f))
            .ShouldKill(ActionId.ChainLightningHit, MeleeDpsA, MeleeDpsB);

    [Test]
    public void LeavingTheWarriorDiesToEmptyDimension()
        => Negative<DsrP5WrathScenario>(CasterDps)
            .Overrides<DsrP5WrathStateOverrides>(Pinned)
            .TeleportAt(W + 27.5f, to: new Vector2(12f, -12f))
            .ShouldKill(ActionId.EmptyDimension, CasterDps);
}
