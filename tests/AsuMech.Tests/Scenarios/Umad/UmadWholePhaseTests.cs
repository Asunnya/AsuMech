using AsuMech.Core.Game.Party;
using AsuMech.Scenarios.Umad.P1TeleTrouncing;
using AsuMech.Scenarios.Umad.P2Trines;
using AsuMech.Scenarios.Umad.P3BlackHole;
using AsuMech.Scenarios.Umad.P3LimitCut;
using AsuMech.Scenarios.Umad.P5Celestriad;
using AsuMech.Scenarios.Umad.P5Exaflares;

namespace AsuMech.Tests.Scenarios.Umad;

public class UmadWholePhaseTests
{
    private const uint Hyperdrive = 0xC24B, TeleTrouncing = 0xBAB9;
    private const uint LightOfJudgmentP2 = 0xBABD, Trine = 0xBADF;
    private const uint Tsunami = 0xBAF5, UmbraSmash = 0xBB00, ThunderIII = 0xBB09, Earthquake = 0xC571;
    private const uint ChaoticFlood = 0xBB4F, Celestriad = 0xBB42, Quake = 0xBB4A, StrayApocalypse = 0xBB3B;

    // From the last resolve of one part to the next part's first boss cast, as retail spaces them.
    // P3's parts follow each other closer than the ~1 s a part change takes, so they get more slack.
    [TestCase(typeof(UmadP1TeleTrouncingScenario), 1, Hyperdrive, TeleTrouncing, 6.91f, 1f)]
    [TestCase(typeof(UmadP2TrinesScenario), 1, LightOfJudgmentP2, Trine, 8.23f, 1f)]
    [TestCase(typeof(UmadP3LimitCutScenario), 1, Tsunami, UmbraSmash, 9.22f, 1.3f)]
    [TestCase(typeof(UmadP3BlackHoleScenario), 2, ThunderIII, Earthquake, 0.09f, 2f)]
    [TestCase(typeof(UmadP5CelestriadScenario), 1, ChaoticFlood, Celestriad, 25.84f, 1f)]
    [TestCase(typeof(UmadP5ExaflaresScenario), 2, Quake, StrayApocalypse, 18.67f, 1f)]
    public void AHandoffWaitsAsLongAsRetail(Type picked, int part, uint lastResolve, uint firstCast, float retail, float tolerance)
    {
        var (run, parts) = WholePhase(picked);
        Assert.That(run.Passed, run.ToString());
        var resolved = parts[part - 1].Last(r => r.Kind == "ability" && r.Id == lastResolve).Global;
        var cast = parts[part].First(r => r.Kind == "cast" && r.Id == firstCast).Global;
        Assert.That(cast - resolved, Is.EqualTo(retail).Within(tolerance));
    }

    [TestCase(typeof(UmadP2TrinesScenario), Trine, UmadP2TrinesScenario.TrineCastAt)]
    [TestCase(typeof(UmadP5CelestriadScenario), Celestriad, 1f)]
    [TestCase(typeof(UmadP5ExaflaresScenario), 0xBB3Bu, 3f)]
    public void APartPlayedOnItsOwnStartsWithoutTheWait(Type scenario, uint firstCast, float at)
    {
        List<Dictionary<string, string>> rows = [];
        var run = ScenarioRun.Execute(scenario, 0, 1, new ScenarioRunOptions
        {
            PlayerRole = PartyRole.MainTank,
            StopAt = at + 1f,
            Finished = fake => rows = fake.Timeline.Rows.ToList(),
        });
        Assert.That(run.Passed, run.ToString());
        var cast = rows.First(r => r["kind"] == "cast" && r["id"] == firstCast.ToString());
        Assert.That(float.Parse(cast["t"], System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo(at).Within(0.1f));
    }

    [TestCase(typeof(UmadP2TrinesScenario), 1)]
    [TestCase(typeof(UmadP5CelestriadScenario), 1)]
    [TestCase(typeof(UmadP5ExaflaresScenario), 2)]
    public void KefkaStandsThroughTheWait(Type picked, int part)
    {
        var (run, parts) = WholePhase(picked);
        Assert.That(run.Passed, run.ToString());
        var start = parts[part].First().Global;
        var spawn = parts[part].First(r => r.Kind == "spawn" && r.ActorName == "Kefka").Global;
        Assert.That(spawn - start, Is.LessThan(0.2f));
    }

    private sealed record Row(float Global, string Kind, uint Id, string ActorName);

    // Rows of each part in order, on one clock running across the whole phase.
    private static (ScenarioRun Run, List<List<Row>> Parts) WholePhase(Type picked)
    {
        var offsets = new List<float>();
        var frames = 0;
        var started = false;
        List<Dictionary<string, string>> rows = [];
        var run = ScenarioRun.Execute(picked, 0, 1, new ScenarioRunOptions
        {
            WholePhase = true,
            PlayerRole = PartyRole.MainTank,
            Started = _ => started = true,
            Probe = p =>
            {
                frames++;
                if (!started) return;
                offsets.Add(frames * ScenarioRun.FrameSeconds - p.Time);
                started = false;
            },
            Finished = fake => rows = fake.Timeline.Rows.ToList(),
        });
        var parts = new List<List<Row>>();
        foreach (var row in rows)
        {
            if (row["kind"] == "commence") parts.Add([]);
            if (parts.Count == 0 || parts.Count > offsets.Count) continue;
            var t = float.Parse(row["t"], System.Globalization.CultureInfo.InvariantCulture);
            uint.TryParse(row["id"], out var id);
            parts[^1].Add(new Row(t + offsets[parts.Count - 1], row["kind"], id, row["actor_name"]));
        }
        return (run, parts);
    }
}
