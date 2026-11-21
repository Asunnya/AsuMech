using AsuMech.Core.Game;
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
    private const float DeltasLastJump = 56.1f;
    private const float SigmasLastJump = 69.8f;
    private static readonly uint[] SolarRays =
        [ActionId.SolarRay, ActionId.SolarRay_7B01, ActionId.SolarRay_81AD, ActionId.SolarRay_7B02];

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
        => RunWhole(picked, 0, seed, null, times);

    private static (ScenarioRun Run, Chain Chain) RunWhole(Type picked, int strat, int seed, Action<ActionResolved, float>? resolved, params (Type, float)[] times)
    {
        var chain = new Chain();
        var listening = false;
        var run = ScenarioRun.Execute(picked, strat, seed, new ScenarioRunOptions
        {
            WholePhase = true,
            PlayerRole = PartyRole.MainTank,
            Probe = p =>
            {
                if (!listening && resolved is not null) p.World.ActionResolved += r => resolved(r, chain.Now);
                listening = true;
                chain.Watch(p, times);
            },
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

    [TestCase(0, 1)]
    [TestCase(0, 4)]
    [TestCase(1, 2)]
    public void P5PlaysTheSolarRaysBetweenItsTriosOnTheFightsBeat(int strat, int seed)
    {
        var rays = new List<(uint Action, PartyRole[] Hits, float At)>();
        var (run, played) = RunWhole(typeof(TopP5DeltaScenario), strat, seed,
            (r, now) => { if (SolarRays.Contains(r.Action.ActionId)) rays.Add((r.Action.ActionId, r.Hits.OfType<ISimPartyMember>().Select(m => m.Role).ToArray(), now)); },
            (typeof(TopP5DeltaScenario), DeltasLastJump), (typeof(TopP5SigmaScenario), SigmasLastJump));
        Assert.That(run.Passed, run.ToString());

        Assert.That(rays.Select(r => r.Action), Is.EqualTo(SolarRays));
        Assert.That(rays.Select(r => r.Hits), Is.EqualTo(new[] { new[] { PartyRole.MainTank }, [PartyRole.OffTank], [PartyRole.MainTank], [PartyRole.OffTank] }));

        var deltaEnds = played.Crossings[(typeof(TopP5DeltaScenario), DeltasLastJump)];
        Assert.That(played.CastStarts[(typeof(TopP5SigmaScenario), ActionId.SolarRay)] - deltaEnds, Is.EqualTo(11.4f).Within(1f), "Delta's last jump to Solar Ray");
        Assert.That(played.CastStarts[(typeof(TopP5SigmaScenario), ActionId.RunMiSigmaVersion)] - rays[1].At, Is.EqualTo(8.4f).Within(0.3f), "second Solar Ray to Sigma");

        var sigmaEnds = played.Crossings[(typeof(TopP5SigmaScenario), SigmasLastJump)];
        Assert.That(played.CastStarts[(typeof(TopP5OmegaScenario), ActionId.SolarRay_81AD)] - sigmaEnds, Is.EqualTo(6.7f).Within(1f), "Sigma's last jump to Solar Ray");
        Assert.That(played.CastStarts[(typeof(TopP5OmegaScenario), ActionId.RunMiOmegaVersion)] - rays[3].At, Is.EqualTo(8.5f).Within(0.3f), "second Solar Ray to Omega");
    }

    [Test]
    public void ASolarRayKillsAnyoneButATankUnderIt()
    {
        var run = ScenarioRun.Execute(typeof(TopP5SigmaScenario), 0, 1, new ScenarioRunOptions
        {
            WholePhase = true,
            PlayerRole = PartyRole.CasterDps,
            WriteArtifactsOnFailure = false,
            Probe = p =>
            {
                if (p.Game.ActiveScenario is TopP5SigmaScenario && p.Time > TopP5SigmaScenario.SolarRayCastAt + 4.2f && p.Time < TopP5SigmaScenario.SolarRayCastAt + 4.8f
                    && p.Member(PartyRole.MainTank) is { } tank && p.Member(PartyRole.CasterDps) is { } caster)
                    caster.SetPosition(tank.Position);
            },
        });
        Assert.That(run.Deaths.Select(d => d.Role), Does.Contain(PartyRole.CasterDps), run.ToString());
        Assert.That(run.Deaths.First(d => d.Role == PartyRole.CasterDps).ActionId, Is.EqualTo(ActionId.SolarRay));
    }

    [TestCase(typeof(TopP5SigmaScenario), ActionId.RunMiSigmaVersion, 3.75f)]
    [TestCase(typeof(TopP5OmegaScenario), ActionId.RunMiOmegaVersion, 1.15f)]
    public void AP5TrioOnItsOwnHasNoSolarRay(Type scenario, uint raidwide, float castAt)
    {
        var casts = new List<(uint Action, float At)>();
        var run = ScenarioRun.Execute(scenario, 0, 1, new ScenarioRunOptions
        {
            PlayerRole = PartyRole.MainTank,
            StopAt = castAt + 1f,
            Probe = p =>
            {
                foreach (var e in p.World.Children.OfType<SimEnemy>().Where(e => e.IsCasting))
                    if (!casts.Any(c => c.Action == e.CastActionId)) casts.Add((e.CastActionId, p.Time));
            },
        });
        Assert.That(run.Passed, run.ToString());
        Assert.That(casts.Select(c => c.Action).Intersect(SolarRays), Is.Empty);
        Assert.That(casts.First(c => c.Action == raidwide).At, Is.EqualTo(castAt).Within(0.05f));
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
    public void P5MusicCarriesOnAcrossItsParts()
    {
        var (run, chain) = RunWhole(typeof(TopP5OmegaScenario), 1);
        Assert.That(run.Passed, run.ToString());
        Assert.That(chain.Starts.Select(s => s.Scenario.GetType()),
            Is.EqualTo(new[] { typeof(TopP5DeltaScenario), typeof(TopP5SigmaScenario), typeof(TopP5OmegaScenario) }));
        for (var i = 1; i < chain.Starts.Count; i++)
        {
            var (_, before, music) = chain.Starts[i - 1];
            Assert.That(chain.Starts[i].Music, Is.EqualTo(music + chain.Starts[i].At - before).Within(1f), chain.Starts[i].Scenario.Name);
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
