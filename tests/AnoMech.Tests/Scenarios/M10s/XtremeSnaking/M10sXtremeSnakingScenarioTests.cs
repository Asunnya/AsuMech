using System.Numerics;
using AnoMech.Scenarios.M10s.XtremeSnaking;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.M10s.M10sConstants;
using static AnoMech.Tests.NegativeRun;
using Scenario = AnoMech.Scenarios.M10s.XtremeSnaking.M10sXtremeSnakingScenario;

namespace AnoMech.Tests;

public class M10sXtremeSnakingScenarioTests
{
    [Test]
    public void TheStratWashesEverySnakeOffForEveryPattern()
    {
        foreach (var pattern in Enumerable.Range(0, M10sXtremeSnakingState.Patterns.Length))
        foreach (var middle in new[] { true, false })
        foreach (var nearest in new[] { true, false })
        {
            var marked = 0;
            var burst = 0;
            var washed = false;
            var run = ScenarioRun.Execute(typeof(Scenario), 0, 5, new ScenarioRunOptions
            {
                Overrides = o =>
                {
                    var overrides = (M10sXtremeSnakingStateOverrides)o;
                    overrides.Pattern = pattern;
                    overrides.RedHotEndsInTheMiddle = middle;
                    overrides.FireOnTheNearest = nearest;
                },
                Probe = p =>
                {
                    if (p.Crossed(Scenario.SnakingHitAt + 0.5f))
                        marked = p.World.Party.ActiveMembers().Count(m => m.HasStatus(StatusId.XtremeFiresnaking) || m.HasStatus(StatusId.XtremeWatersnaking));
                    if (p.Crossed(Scenario.StopAt[0] + Scenario.StopToHit + 1.8f))
                        burst = p.World.Party.ActiveMembers().Count(m => m.HasStatus(StatusId.MagicVulnerabilityUp));
                    if (!p.Crossed(Scenario.StopAt[^1] + Scenario.StopToHit + 1f)) return;
                    Assert.That(p.World.Party.ActiveMembers().Where(m => m.HasStatus(StatusId.XtremeFiresnaking) || m.HasStatus(StatusId.XtremeWatersnaking)), Is.Empty,
                        $"pattern {pattern}, middle {middle}, nearest {nearest}");
                    washed = true;
                },
            });
            Assert.That(run.Passed, run.ToString());
            Assert.That(marked, Is.EqualTo(8));
            Assert.That(burst, Is.EqualTo(8), "each washed snake's Bailout catches its whole group");
            Assert.That(washed, Is.True);
        }
    }

    // Two players on one Snap cone each take both cones.
    [Test]
    public void SharingASnapConeKillsBoth()
        => Negative<Scenario>(MainTank)
            .Overrides<M10sXtremeSnakingStateOverrides>(o => { o.Pattern = 1; o.FireOnTheNearest = true; o.RedHotEndsInTheMiddle = true; })
            .TeleportAt(Scenario.StopAt[0] - 0.5f, to: new Vector2(1.72f, -17.16f))
            .ShouldKill(ActionId.PlungingSnapCone, MainTank, MeleeDpsA);
}
