using System.Linq;
using AnoMech.Learn.Mitigation;
using NUnit.Framework;

namespace AnoMech.Tests.Learn.Mitigation;

// Every embedded plan stays importable: anchors resolve on the embedded timelines, every accepted
// action is in the catalog, and only the rows known to have no cast stay unresolved.
public class MitPlanValidationTests
{
    private static readonly string[] KnownUnresolved =
    [
        "Cleanses: no cast named like \"Cleanses\"",
        "Infernal Nails (Spawn): no cast named like \"Infernal Nails (Spawn)\"",
        "Rock Slide & Mountain Buster: Mountain Buster has no occurrence 3",
    ];

    [Test]
    public void EmbeddedPlansExist()
        => Assert.That(MitTimelines.PlanNames(), Does.Contain("uwu-ethel-master"));

    [Test]
    public void AnchorsResolveAndAcceptedActionsAreKnown()
    {
        foreach (var name in MitTimelines.PlanNames())
        {
            var plan = MitTimelines.LoadPlan(name)!;
            foreach (var (scenarioType, scenario) in plan.Scenarios)
            {
                var timeline = MitTimelines.Load(scenarioType);
                Assert.That(timeline, Is.Not.Null, $"{name}: no timeline for {scenarioType}");
                foreach (var mech in scenario.Mechanics)
                {
                    if (mech.Anchor is { } a) Assert.That(timeline!.Find(a.Cast, a.Occurrence), Is.Not.Null, $"{name}/{scenarioType}: {mech.Label}");
                    foreach (var item in mech.Columns.Values.SelectMany(i => i))
                    foreach (var accept in item.Accept ?? [])
                        Assert.That(MitCatalog.Find(accept), Is.Not.Null, $"{name}/{scenarioType}: {mech.Label} accepts unknown {accept}");
                }
            }
        }
    }

    [Test]
    public void OnlyTheKnownRowsStayUnresolved()
    {
        var plan = MitTimelines.LoadPlan("uwu-ethel-master")!;
        Assert.That(plan.UnresolvedTexts(), Is.EquivalentTo(KnownUnresolved));
    }

    [Test]
    public void EveryColumnResolvesToSomethingForItsJobs()
    {
        var plan = MitTimelines.LoadPlan("uwu-ethel-master")!;
        var timeline = MitTimelines.Load("UwuP1GarudaScenario")!;
        foreach (var (column, job) in new[] { ("MT", "WAR"), ("OT", "PLD"), ("Scholar", "SCH"), ("Sage", "SGE"), ("White Mage", "WHM"), ("Astrologian", "AST"), ("Melee 1", "NIN"), ("Caster", "RDM") })
            Assert.That(MitPlanResolver.Resolve(plan.Scenarios["UwuP1GarudaScenario"], timeline, column, job), Is.Not.Empty, column);
    }
}
