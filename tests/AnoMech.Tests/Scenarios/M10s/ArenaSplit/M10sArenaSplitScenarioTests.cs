using System.Numerics;
using AnoMech.Scenarios.M10s.ArenaSplit;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.M10s.M10sConstants;
using static AnoMech.Tests.NegativeRun;
using Scenario = AnoMech.Scenarios.M10s.ArenaSplit.M10sArenaSplitScenario;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M10sArenaSplitScenarioTests
{
    private static void Pinned(M10sArenaSplitStateOverrides o, int waveFrom = 3)
    {
        o.DeepBlueWest = true;
        o.DoubleDip = true;
        o.WaveFrom = waveFrom;
        o.PlayerPyrotation = true;
    }

    [Test]
    public void StandingOnTheMiddleLineWhenRedHotChargesKills()
        => Negative<Scenario>(MeleeDpsB)
            .Overrides<M10sArenaSplitStateOverrides>(o => Pinned(o))
            .TeleportAt(Scenario.SplitLineAt - 1f, to: new Vector2(0f, 5f))
            .ShouldKill(ActionId.FlameFloaterSplitLine, MeleeDpsB);

    [Test]
    public void WalkingIntoTheSplitFireBurnsToDeath()
        => Negative<Scenario>(MeleeDpsB)
            .Overrides<M10sArenaSplitStateOverrides>(o => Pinned(o))
            .TeleportAt(Scenario.SplitFireAt + 2f, to: new Vector2(1f, 8f))
            .ShouldKill(ActionId.FlameFloaterSplit, MeleeDpsB);

    [Test]
    public void TakingFreakyPyrotationAloneKills()
        => Negative<Scenario>(CasterDps)
            .Overrides<M10sArenaSplitStateOverrides>(o => Pinned(o))
            .TeleportAt(Scenario.PyrotationHitAt - 1.5f, to: new Vector2(15f, -5f))
            .ShouldKill(ActionId.FreakyPyrotationStack, CasterDps);

    [Test]
    public void StandingInDeepBluesLaneKills()
        => Negative<Scenario>(OffTank)
            .Overrides<M10sArenaSplitStateOverrides>(o => Pinned(o, waveFrom: 0))
            .TeleportAt(Scenario.WaveHitsAt - 2f, to: new Vector2(6f, -5f))
            .ShouldKill(ActionId.SickestTakeoffLane, OffTank);

    [Test]
    public void TheMiddleBurstsAfterTheWave()
        => Negative<Scenario>(OffTank)
            .Overrides<M10sArenaSplitStateOverrides>(o => Pinned(o))
            .TeleportAt(Scenario.SlabAt + 0.8f, to: new Vector2(6f, 0f))
            .ShouldKill(ActionId.SteamBurst, OffTank);

    [Test]
    public void StandingStillThroughDoubleDipLeavesDamageDown()
    {
        var checkedIt = false;
        var run = ScenarioRun.Execute(typeof(Scenario), 0, 7, new ScenarioRunOptions
        {
            PlayerRole = MainTank,
            Overrides = o => Pinned((M10sArenaSplitStateOverrides)o),
            Takeovers = [new Takeover(Scenario.FirstConesAt + 0.1f, null)],
            Probe = p =>
            {
                if (!p.Crossed(Scenario.SecondConesAt + 1f)) return;
                Assert.That(p.Member(MainTank)!.HasStatus(StatusId.DamageDown), Is.True);
                checkedIt = true;
            },
        });
        Assert.That(checkedIt, Is.True, run.ToString());
    }

    [Test]
    public void TheStratClearsBothConeSetsFromEitherSide()
    {
        foreach (var doubleDip in new[] { true, false })
        foreach (var west in new[] { true, false })
        {
            var checkedIt = false;
            var run = ScenarioRun.Execute(typeof(Scenario), 0, 11, new ScenarioRunOptions
            {
                Overrides = o =>
                {
                    var overrides = (M10sArenaSplitStateOverrides)o;
                    overrides.DoubleDip = doubleDip;
                    overrides.DeepBlueWest = west;
                },
                Probe = p =>
                {
                    if (!p.Crossed(Scenario.SecondConesAt + 1f)) return;
                    Assert.That(p.World.Party.ActiveMembers().Where(m => m.HasStatus(StatusId.DamageDown)), Is.Empty, $"double-dip {doubleDip}, west {west}");
                    checkedIt = true;
                },
            });
            Assert.That(run.Passed, run.ToString());
            Assert.That(checkedIt, Is.True);
        }
    }

    [Test]
    public void WaterCrossingTheFireBurstsWhereItCrosses()
    {
        Assert.That(Scenario.StripCrossing(new Vector3(-12f, 0f, 0f), MathF.PI / 2f)!.Value.Z, Is.EqualTo(0f).Within(1e-3f));
        Assert.That(Scenario.StripCrossing(new Vector3(-12f, 0f, 0f), -MathF.PI / 2f), Is.Null);
        var crossing = Scenario.StripCrossing(new Vector3(-12f, 0f, 0f), MathF.Atan2(17f, -19f))!.Value;
        Assert.That(crossing.Z, Is.EqualTo(-12f * 19f / 17f).Within(1e-3f));
    }
}
