using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Top.P3HelloWorld;
using AsuMech.Scenarios.Top.P3Intermission;
using AsuMech.Scenarios.Top.P3Monitors;
using AsuMech.Scenarios.Top.P5Delta;
using AsuMech.Scenarios.Top.P5Omega;
using AsuMech.Scenarios.Top.P5Sigma;
using static AsuMech.Scenarios.Top.TopConstants;

namespace AsuMech.Tests.Scenarios.Top;

public class TopWholePhaseTests
{
    private const float ColossalBlowsResolve = 29.63f;
    private const float CriticalErrorResolves = 116.43f;
    private const float P3TrackStarts = 13.03f;

    // Run-wide clock of a Whole phase run, and when each boss cast first started on it.
    private sealed class Chain
    {
        public float Now;
        public readonly List<(IScenario Scenario, float At, float Music)> Starts = [];
        public readonly Dictionary<(Type, uint), float> CastStarts = [];
        public readonly Dictionary<(Type, float), float> Crossings = [];
        private readonly HashSet<SimEnemy> casting = [];

        public void Watch(ScenarioProbe p, params (Type Part, float Time)[] times)
        {
            Now += ScenarioRun.FrameSeconds;
            if (p.Game.ActiveScenario is not { } active) return;
            // A delayed part starts its track earlier by the delay.
            if (Starts.Count == 0 || Starts[^1].Scenario != active)
                Starts.Add((active, Now, active.BgmSecondsAtStart + MathF.Min(0f, p.Time)));
            foreach (var (part, time) in times)
                if (active.GetType() == part && p.Crossed(time)) Crossings.TryAdd((part, time), Now);
            foreach (var enemy in p.World.Children.OfType<SimEnemy>())
            {
                if (!enemy.IsCasting) casting.Remove(enemy);
                else if (casting.Add(enemy)) CastStarts.TryAdd((active.GetType(), enemy.CastActionId), Now);
            }
        }
    }

    private static (ScenarioRun Run, Chain Chain) RunWhole(Type picked, int seed, params (Type, float)[] times)
    {
        var chain = new Chain();
        var run = ScenarioRun.Execute(picked, 0, seed, new ScenarioRunOptions
        {
            WholePhase = true,
            PlayerRole = PartyRole.MainTank,
            Probe = p => chain.Watch(p, times),
        });
        return (run, chain);
    }

    [TestCase(1)]
    [TestCase(7)]
    [TestCase(8)]
    public void P3HandsOverOnTheFightsBeat(int seed)
    {
        var (run, chain) = RunWhole(typeof(TopP3MonitorsScenario), seed,
            (typeof(TopP3IntermissionScenario), ColossalBlowsResolve), (typeof(TopP3HelloWorldScenario), CriticalErrorResolves));
        Assert.That(run.Passed, run.ToString());

        var blows = chain.Crossings[(typeof(TopP3IntermissionScenario), ColossalBlowsResolve)];
        var helloWorld = chain.CastStarts[(typeof(TopP3HelloWorldScenario), ActionId.HelloWorld)];
        Assert.That(helloWorld - blows, Is.EqualTo(7.66f).Within(1f), "last Colossal Blow to Hello World");

        var criticalError = chain.Crossings[(typeof(TopP3HelloWorldScenario), CriticalErrorResolves)];
        var monitor = chain.CastStarts.Where(c => c.Key.Item1 == typeof(TopP3MonitorsScenario)
            && c.Key.Item2 is ActionId.OversampledWaveCannonLeft or ActionId.OversampledWaveCannonRight).Select(c => c.Value).Single();
        Assert.That(monitor - criticalError, Is.EqualTo(11.50f).Within(0.3f), "Critical Error to Oversampled Wave Cannon");
    }

    [Test]
    public void P3MusicCarriesOnAcrossItsParts()
    {
        var (run, chain) = RunWhole(typeof(TopP3MonitorsScenario), 1);
        Assert.That(run.Passed, run.ToString());
        var trackStarted = chain.Starts[0].At + P3TrackStarts;
        foreach (var (scenario, at, music) in chain.Starts.Skip(1))
            Assert.That(music, Is.EqualTo(at - trackStarted).Within(1f), scenario.Name);
    }

    [Test]
    public void P5MusicNeverGoesBackAcrossItsParts()
    {
        var (run, chain) = RunWhole(typeof(TopP5OmegaScenario), 1);
        Assert.That(run.Passed, run.ToString());
        Assert.That(chain.Starts.Select(s => s.Scenario.GetType()),
            Is.EqualTo(new[] { typeof(TopP5DeltaScenario), typeof(TopP5SigmaScenario), typeof(TopP5OmegaScenario) }));
        for (var i = 1; i < chain.Starts.Count; i++)
        {
            var (_, before, music) = chain.Starts[i - 1];
            Assert.That(chain.Starts[i].Music, Is.GreaterThanOrEqualTo(music + chain.Starts[i].At - before), chain.Starts[i].Scenario.Name);
        }
    }

    [Test]
    public void HelloWorldOnItsOwnKeepsItsLeadIn()
    {
        var castAt = (float?)null;
        var run = ScenarioRun.Execute(typeof(TopP3HelloWorldScenario), 0, 1, new ScenarioRunOptions
        {
            PlayerRole = PartyRole.MainTank,
            StopAt = 5f,
            Probe = p =>
            {
                if (castAt is null && p.World.Children.OfType<SimEnemy>().Any(e => e.IsCasting && e.CastActionId == ActionId.HelloWorld))
                    castAt = p.Time;
            },
            Started = s => Assert.That(s.BgmSecondsAtStart, Is.EqualTo(21.29f)),
        });
        Assert.That(run.Passed, run.ToString());
        Assert.That(castAt, Is.EqualTo(3.00f).Within(0.05f));
    }
}
