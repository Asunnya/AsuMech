using System.Linq;
using AsuMech.Core.Game.Party;
using AsuMech.Learn.Mitigation;
using NUnit.Framework;

namespace AsuMech.Tests.Learn.Mitigation;

public class MitStatusesTests
{
    private static readonly DataminingGameData Data = new(new FakeRsvFunctions());

    [Test]
    public void EveryStatusIdNamesTheStatusTheTableSays()
    {
        var wrong = MitStatuses.All.Select(m => (m, Row: Data.Status(m.StatusId))).Where(x => x.Row is null || x.Row.Name != x.m.StatusName)
            .Select(x => $"{x.m.StatusId} {x.m.StatusName} -> {x.Row?.Name ?? "missing"}").ToList();
        Assert.That(wrong, Is.Empty);
    }

    [Test]
    public void EveryActionIsInTheCatalogAndItsStatusCountsForDamage()
    {
        Assert.That(MitStatuses.All.Where(m => MitCatalog.Find(m.Action) is null).Select(m => m.Action), Is.Empty);
        Assert.That(MitStatuses.All.Where(m => !AsuMech.Core.UserActions.Mitigation.ByStatusId.ContainsKey(m.StatusId)).Select(m => m.StatusName), Is.Empty);
        Assert.That(MitStatuses.ForAction(MitCatalog.Find("Reprisal")!.Id)!.StatusId, Is.EqualTo(1193));
    }

    [Test]
    public void TheBotsColumnsOfTheGarudaPlanBecomeTimedStatuses()
    {
        var plan = MitTimelines.LoadPlan("uwu-ethel-master")!;
        var timeline = MitTimelines.Load("UwuP1GarudaScenario")!;
        var jobs = new System.Collections.Generic.Dictionary<PartyRole, string>
        {
            [PartyRole.MainTank] = "WAR", [PartyRole.OffTank] = "PLD", [PartyRole.RegenHealer] = "WHM", [PartyRole.ShieldHealer] = "SCH",
            [PartyRole.MeleeDpsA] = "NIN", [PartyRole.MeleeDpsB] = "SAM", [PartyRole.PhysRangedDps] = "BRD", [PartyRole.CasterDps] = "RDM",
        };
        var planned = PlannedMits.Schedule(plan.Scenarios["UwuP1GarudaScenario"], timeline, jobs, PartyRole.MeleeDpsA);
        TestContext.Out.WriteLine(string.Join("\n", planned.Select(p => $"{p.Time,6:0.0}s {p.Role,-14} {p.Action,-22} -> {p.Target} {p.Ally}")));
        Assert.That(planned, Is.Not.Empty);
        Assert.That(planned.Select(p => p.Role), Has.None.EqualTo(PartyRole.MeleeDpsA), "the player's own column is not played by a bot");
        Assert.That(planned.Select(p => p.Role).Distinct().Count(), Is.GreaterThanOrEqualTo(3));
        Assert.That(planned, Is.Ordered.By("Time"));
        Assert.That(planned.All(p => p.Time > 0f), Is.True);
        Assert.That(planned.Any(p => p.Target == MitTarget.Enemy), "a Reprisal or Feint goes on the boss");
    }
}
