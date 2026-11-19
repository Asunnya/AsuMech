using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Scenarios.Umad.P3DecisiveBattle;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Tests.NegativeRun;
using ActionId = AsuMech.Scenarios.Umad.UmadConstants.ActionId;
using Scenario = AsuMech.Scenarios.Umad.P3DecisiveBattle.UmadP3DecisiveBattleScenario;

namespace AsuMech.Tests;

// The wind crystal pinned north-west: Chaos stands at (-6.9, -6.9) facing out, the party gathers
// at (-12.4, -12.4), and the Entropy holders spread 7y either side of it.
public class UmadP3DecisiveBattleScenarioTests
{
    private static void WindNorthWest(UmadP3DecisiveBattleStateOverrides o) => o.WindCrystalSpot = 3;

    [Test]
    public void StayingOnExdeathForHisThunderKills()
        => Negative<Scenario>(OffTank)
            .Overrides<UmadP3DecisiveBattleStateOverrides>(WindNorthWest)
            .FreezeAt(Scenario.PbaoeCastAt)
            .ShouldKill(ActionId.ThunderIII_Pbaoe, OffTank);

    [Test]
    public void StandingBesideChaosForALatitudinalImplosionKills()
        => Negative<Scenario>(RegenHealer)
            .Overrides<UmadP3DecisiveBattleStateOverrides>(o => { WindNorthWest(o); o.Latitudinal = true; })
            .TeleportAt(Scenario.ShockwaveAt[0] - 0.3f, to: new Vector2(-3.3f, -10.4f))
            .ShouldKill(ActionId.Shockwave, RegenHealer);

    [Test]
    public void TwoEntropyHoldersStandingTogetherBothDie()
        => Negative<Scenario>(RegenHealer)
            .Overrides<UmadP3DecisiveBattleStateOverrides>(o => { WindNorthWest(o); o.FireShort = true; o.EntropyOn = RegenHealer; })
            .TeleportAt(Scenario.ShortDebuffsAt - 0.3f, to: new Vector2(-7.4f, -17.3f))
            .ShouldKillSomeone(ActionId.StrayFlames_Entropy);

    [Test]
    public void ExdeathsSecondBusterNeedsAnInvuln()
        => Negative<Scenario>(OffTank)
            .Overrides<UmadP3DecisiveBattleStateOverrides>(WindNorthWest)
            .WithMitigationChecks()
            .TeleportAt(Scenario.BusterAt[0] - 1f, to: new Vector2(-1f, 1.6f))
            .ShouldKill(ActionId.ThunderIII_Resolve, OffTank);

    [Test]
    public void StraySprayDonutIsSafeOnlyWithinFourYalms()
    {
        AoeQuery? donut = null;
        void Capture(AoeQuery q)
        {
            if (q.ActionId == ActionId.StraySpray_DynamicFluid) donut ??= q;
        }
        AoeQuery.Evaluated += Capture;
        try
        {
            var run = ScenarioRun.Execute(typeof(Scenario), 0, 1);
            Assert.That(run.Passed, run.ToString());
            Assert.That(donut, Is.Not.Null);
            var centre = donut!.Value.Source.Position;
            Assert.That(donut.Value.SignedDistance(centre + new Vector3(3.5f, 0f, 0f)), Is.GreaterThan(0f), "inside the safe hole");
            Assert.That(donut.Value.SignedDistance(centre + new Vector3(5f, 0f, 0f)), Is.LessThan(0f), "5y out");
        }
        finally
        {
            AoeQuery.Evaluated -= Capture;
        }
    }
}
