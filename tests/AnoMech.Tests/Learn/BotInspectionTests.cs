using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Learn;
using AnoMech.Scenarios.M11s.Opener;

namespace AnoMech.Tests;

public class BotInspectionTests
{
    [Test]
    public void EverySeatKeepsTheLastOrderTheStratGaveIt()
    {
        Dictionary<PartyRole, GuidanceOrder>? seen = null;
        var run = ScenarioRun.Execute(typeof(M11sOpenerScenario), 0, 1, new ScenarioRunOptions
        {
            StopAt = 30f,
            Probe = p => { if (p.Crossed(29f)) seen = p.World.LastOrders.ToDictionary(kv => kv.Key, kv => kv.Value); },
        });
        Assert.That(run.Passed, run.Failure);
        Assert.That(seen, Is.Not.Null);
        Assert.That(seen!.Keys, Is.EquivalentTo(Enum.GetValues<PartyRole>()), "bots too, not only the player's seat");
        Assert.That(seen.Values.All(o => o.Key.StratClass == "M11sOpenerAi" && o.Key.Source.StartsWith("M11sOpenerAi.cs:")));
        Assert.That(seen.Values.All(o => o.IssuedAt <= 29f));
    }

    [Test]
    public void TheDescriptionNamesTheCallTheSpotAndTheLessonsRule()
    {
        var key = new GuidanceKey("M11sOpenerAi", 23.6f, "M11sOpenerAi.cs:40", "RawSteelSpot");
        var order = new GuidanceOrder(key, PartyRole.OffTank, new Vector3(3f, 0f, -4f), 23.6f, 26f, Sprint: true);
        var lesson = new Lesson("M11sOpenerScenario", "Hector", [], [
            new LessonStep(new LessonStepKey("M11sOpenerAi", Time: 23.6f), "Raw Steel", Cue: "Trophy cast",
                Rule: "Tanks take the cones", Roles: new() { ["OffTank"] = new LessonRoleText(Rule: "Off tank: the left cone") }),
        ]);
        var lines = BotInspection.Describe(PartyRole.OffTank, order, new Vector3(3f, 0f, 0f), 25f, lesson).ToDictionary(l => l.Label, l => l.Text);
        Assert.That(lines["Seat"], Is.EqualTo("OffTank"));
        Assert.That(lines["Call"], Is.EqualTo("M11sOpenerAi.RawSteelSpot at 23.6s (M11sOpenerAi.cs:40)"));
        Assert.That(lines["Issued"], Is.EqualTo("23.6s, 1.4s ago, sprinting"));
        Assert.That(lines["Spot"], Is.EqualTo("(3, -4), 4 y away, due by 26s"));
        Assert.That(lines["Step"], Is.EqualTo("Raw Steel"));
        Assert.That(lines["Rule"], Is.EqualTo("Off tank: the left cone"), "the seat's own rule wins");
        Assert.That(lines["Cue"], Is.EqualTo("Trophy cast"));
        Assert.That(BotInspection.Describe(PartyRole.MainTank, null, Vector3.Zero, 0f, null).Single(l => l.Label == "Order").Text, Does.StartWith("none yet"));
    }
}
