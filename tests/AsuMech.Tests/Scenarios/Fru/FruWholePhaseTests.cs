using System.Globalization;
using AsuMech.Core.Game.Party;
using AsuMech.Core.Native.Interfaces;
using AsuMech.Scenarios.Fru.P3Apocalypse;
using AsuMech.Scenarios.Fru.P4CrystallizeTime;
using AsuMech.Scenarios.Fru.P5ParadiseLost;
using static AsuMech.Scenarios.Fru.FruConstants;

namespace AsuMech.Tests;

// Whole phase handoffs against retail: from the previous part's last hit to the next part's first
// boss cast, and the music carrying on across them.
public class FruWholePhaseTests
{
    private const float Tolerance = 1f;

    private sealed record Row(int Part, float At, string Kind, uint Id, string Detail);

    private sealed record WholeRun(ScenarioRun Run, List<Row> Rows)
    {
        public float LastHit(int part, uint id) => Rows.Last(r => r.Part == part && r.Kind == "ability" && r.Id == id).At;
        public float FirstCast(int part, uint id) => Rows.First(r => r.Part == part && r.Kind == "cast" && r.Id == id).At;
        public float Gap(int part, uint lastHit, uint firstCast) => FirstCast(part + 1, firstCast) - LastHit(part, lastHit);
    }

    // Rows on one clock across the parts, each tagged with the part it was sent in.
    private static WholeRun PlayWhole(Type scenario, int seed)
    {
        var frames = 0;
        var part = -1;
        var partOfRow = new List<int>();
        List<Dictionary<string, string>> raw = [];
        var run = ScenarioRun.Execute(scenario, 0, seed, new ScenarioRunOptions
        {
            WholePhase = true,
            PlayerRole = PartyRole.MainTank,
            Configure = c => c.SuppressBgm = false,
            Started = _ =>
            {
                var timeline = ((FakeBattleCharas)Natives.BattleCharas).Timeline;
                timeline.Clock = () => frames * ScenarioRun.FrameSeconds;
                while (partOfRow.Count < timeline.Rows.Count - 1) partOfRow.Add(part);
                part++;
            },
            Probe = _ => frames++,
            Finished = fake => raw = fake.Timeline.Rows.ToList(),
        });
        while (partOfRow.Count < raw.Count) partOfRow.Add(part);
        var rows = raw.Select((r, i) => new Row(partOfRow[i], float.Parse(r["t"], CultureInfo.InvariantCulture), r["kind"],
            uint.TryParse(r["id"], out var id) ? id : 0, r["detail"])).ToList();
        return new WholeRun(run, rows);
    }

    // Each part's music against where the previous part's track had got to; parts listed in
    // `jumps` are known to skip.
    private static void AssertMusicCarriesOn(WholeRun whole, params int[] jumps)
    {
        var plays = whole.Rows.Where(r => r.Kind == "bgm" && r.Detail.StartsWith("seconds_in=")).ToList();
        Assert.That(plays.Select(p => p.Part).Distinct().Count(), Is.EqualTo(whole.Rows.Max(r => r.Part) + 1), "a play per part");
        for (var i = 1; i < plays.Count; i++)
        {
            if (jumps.Contains(plays[i].Part)) continue;
            var carried = float.Parse(plays[i - 1].Detail["seconds_in=".Length..], CultureInfo.InvariantCulture) + plays[i].At - plays[i - 1].At;
            var started = float.Parse(plays[i].Detail["seconds_in=".Length..], CultureInfo.InvariantCulture);
            Assert.That(started, Is.EqualTo(carried).Within(Tolerance), $"music at part {plays[i].Part}");
        }
    }

    [TestCase(1)]
    [TestCase(7)]
    public void P3RelativityToApocalypseWaitsOutBlackHalo(int seed)
    {
        var whole = PlayWhole(typeof(FruP3ApocalypseScenario), seed);
        Assert.Multiple(() =>
        {
            Assert.That(whole.Run.Passed, whole.Run.ToString);
            Assert.That(whole.Gap(0, ActionId.ShockwavePulsar, ActionId.SpellInWaitingRefrain), Is.EqualTo(15.6f).Within(Tolerance));
            Assert.That(whole.FirstCast(1, ActionId.BlackHalo) - whole.LastHit(0, ActionId.ShockwavePulsar), Is.EqualTo(3.2f).Within(Tolerance));
            Assert.That(whole.Rows.Single(r => r.Kind == "ability" && r.Id == ActionId.BlackHalo).Detail, Does.StartWith("hits=2 "));
            AssertMusicCarriesOn(whole);
        });
    }

    [Test]
    public void ApocalypsePlayedAloneHasNoBlackHalo()
    {
        var rows = CastTimelines.SimRows(typeof(FruP3ApocalypseScenario), 0, 1, out var failure);
        Assert.That(failure, Is.Null);
        Assert.That(rows.Where(r => r["id"] == ActionId.BlackHalo.ToString()), Is.Empty);
        Assert.That(rows.First(r => r["kind"] == "cast")["id"], Is.EqualTo(ActionId.SpellInWaitingRefrain.ToString()));
        Assert.That(float.Parse(rows.First(r => r["kind"] == "cast")["t"], CultureInfo.InvariantCulture), Is.EqualTo(2.1f).Within(0.05f));
    }

    [TestCase(1)]
    [TestCase(7)]
    public void P4HandoffsMatchRetail(int seed)
    {
        var whole = PlayWhole(typeof(FruP4CrystallizeTimeScenario), seed);
        Assert.Multiple(() =>
        {
            Assert.That(whole.Run.Passed, whole.Run.ToString);
            Assert.That(whole.Gap(0, ActionId.Materialization, ActionId.AkhRhaiCast), Is.EqualTo(11.2f).Within(Tolerance));
            Assert.That(whole.Gap(1, ActionId.MornAfahUsurper, ActionId.CrystallizeTimeUsurper), Is.EqualTo(5.6f).Within(Tolerance));
            AssertMusicCarriesOn(whole, 1);
        });
    }

    // Fulgent Blade I to Paradise Regained I runs 4.6 s long: retail starts Paradise Regained while
    // the exalines still roll, and a part only hands off once its own events are done.
    [TestCase(1)]
    [TestCase(7)]
    public void P5HandoffsMatchRetail(int seed)
    {
        var whole = PlayWhole(typeof(FruP5ParadiseLostScenario), seed);
        Assert.Multiple(() =>
        {
            Assert.That(whole.Run.Passed, whole.Run.ToString);
            Assert.That(whole.Gap(1, ActionId.CruelPathOfLightEcho, ActionId.PandorasBox), Is.EqualTo(5.9f).Within(Tolerance));
            Assert.That(whole.Gap(2, ActionId.PandorasBox, ActionId.FulgentBlade), Is.EqualTo(6.3f).Within(Tolerance));
            Assert.That(whole.Gap(3, ActionId.AkhMornPandoraLeft, ActionId.ParadiseRegained), Is.EqualTo(8.4f).Within(Tolerance));
            Assert.That(whole.Gap(4, ActionId.CruelPathOfLightEcho, ActionId.FulgentBlade), Is.EqualTo(2.7f).Within(Tolerance));
            Assert.That(whole.Gap(5, ActionId.AkhMornPandoraLeft, ActionId.ParadiseLost), Is.EqualTo(8.4f).Within(Tolerance));
            AssertMusicCarriesOn(whole, 1);
        });
    }
}
