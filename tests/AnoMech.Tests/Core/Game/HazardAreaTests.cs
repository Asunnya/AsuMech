using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Scenarios.M11s;
using AnoMech.Scenarios.M11s.Opener;
using AnoMech.Scenarios.M11s.OrbitalOmen;

namespace AnoMech.Tests;

public class HazardAreaTests
{
    private sealed record Seen(string Name, string Shape, float ResolveIn, int Loops, int Points, float? OriginDistance);

    // Every area a run announced before resolving it, by shape, with what the overlay would read.
    private static (ScenarioRun Run, Dictionary<string, Seen> ByShape, bool EverStale) Observe(Type scenario)
    {
        var byShape = new Dictionary<string, Seen>();
        var everStale = false;
        var run = ScenarioRun.Execute(scenario, 0, 1, new ScenarioRunOptions
        {
            WriteArtifactsOnFailure = false,
            Probe = probe =>
            {
                foreach (var hazard in probe.World.Hazards)
                {
                    if (hazard.ResolveAt < probe.Time - 0.1f) everStale = true;
                    var loops = hazard.Outline();
                    byShape.TryAdd(hazard.ShapeWord, new Seen(hazard.Name, hazard.ShapeWord, hazard.ResolveAt - probe.Time, loops.Count, loops.Sum(l => l.Count),
                        hazard.SignedDistance(hazard.Origin.Position)));
                }
            },
        });
        return (run, byShape, everStale);
    }

    [Test]
    public void ConesAndLinesAreAnnouncedWhileTheirBarRuns()
    {
        var (run, seen, stale) = Observe(typeof(M11sOrbitalOmenScenario));
        Assert.That(run.Passed, run.Failure ?? string.Join("; ", run.Deaths.Select(d => d.Cause)));
        Assert.That(seen.Keys, Is.SupersetOf(new[] { "cone", "line" }), string.Join(", ", seen.Values.Select(s => $"{s.Shape}:{s.Name}")));
        var cone = seen["cone"];
        Assert.That(cone.ResolveIn, Is.GreaterThan(0f).And.LessThanOrEqualTo(6f), "seen while the cast bar runs, resolving when it ends");
        Assert.That(cone.Loops, Is.EqualTo(1));
        Assert.That(cone.Points, Is.GreaterThan(3), "apex plus an arc");
        Assert.That(cone.OriginDistance, Is.LessThanOrEqualTo(0f), "the apex is inside its own cone");
        var line = seen["line"];
        Assert.That((line.Loops, line.Points), Is.EqualTo((1, 4)));
        Assert.That(stale, Is.False, "an area never outlives its resolve");
        Assert.That(run.Elapsed, Is.GreaterThan(10f));
    }

    [Test]
    public void DonutsAndCirclesKeepTheirHole()
    {
        var (run, seen, _) = Observe(typeof(M11sOpenerScenario));
        Assert.That(run.Passed, run.Failure ?? string.Join("; ", run.Deaths.Select(d => d.Cause)));
        Assert.That(seen.Keys, Does.Contain("donut").And.Contain("circle"), string.Join(", ", seen.Values.Select(s => $"{s.Shape}:{s.Name}")));
        var donut = seen["donut"];
        Assert.That(donut.Loops, Is.EqualTo(2), "outer edge and the hole");
        Assert.That(donut.OriginDistance, Is.GreaterThan(0f), "the centre of a donut is safe");
        Assert.That(seen["circle"].OriginDistance, Is.LessThan(0f));
    }

    [Test]
    public void TheWorldForgetsAreasWhenTheRunEnds()
    {
        SimWorldAfter(typeof(M11sOpenerScenario));
    }

    private static void SimWorldAfter(Type scenario)
    {
        AnoMech.Core.SimObjects.SimWorld? world = null;
        var run = ScenarioRun.Execute(scenario, 0, 1, new ScenarioRunOptions { WriteArtifactsOnFailure = false, Probe = p => world = p.World });
        Assert.That(run.Passed);
        Assert.That(world!.Hazards, Is.Empty);
    }

    [Test]
    public void ThePhraseSaysHowFarInsideAndWhichShape()
    {
        FakeGame.Install();
        var cone = new AoeQuery(M11sConstants.ActionId.HeavyHitter, new Placement(Vector3.Zero, 0f), 0f, M11sActions.HeavyHitterHalfAngle);
        Assert.That(HitExplanation.Phrase("Heavy Hitter", cone, new Vector3(0f, 0f, 5f)), Is.EqualTo("Heavy Hitter: you were 1.9 yalms inside the cone"));
        Assert.That(HitExplanation.Phrase("Heavy Hitter", cone, new Vector3(10f, 0f, -5f)), Is.EqualTo("Heavy Hitter: you were caught at the cone's edge"));
        var circle = new AoeQuery(M11sConstants.ActionId.Impact, new Placement(new Vector3(100f, 0f, 100f), 0f));
        Assert.That(HitExplanation.Phrase("Impact", circle, new Vector3(100f, 0f, 102f)), Is.EqualTo("Impact: you were 4.0 yalms inside the circle"));
        var attack = new AoeQuery(M11sConstants.ActionId.Attack, new Placement(Vector3.Zero, 0f));
        Assert.That(HitExplanation.Phrase("attack", attack, Vector3.Zero), Is.EqualTo("attack: it was aimed at you"));
        Assert.That(HazardArea.ShapeWordOf(CastType.Rectangle), Is.EqualTo("line"));
    }
}
