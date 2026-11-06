using System.Collections.Generic;
using System.Linq;
using AsuMech.Learn.Mitigation;
using NUnit.Framework;

namespace AsuMech.Tests.Learn.Mitigation;

public class MitPlanResolverTests
{
    private static MitScenario Garuda => MitSheetImport.Import(CastTimelineTests.Fixture("ethel-garuda.csv"), CastTimelineTests.Garuda, MitAliases.Default);

    [Test]
    public void HealersPickTheirJobsColumnOthersTheirSeat()
    {
        Assert.That(MitPlanResolver.ColumnFor("SCH", "RegenHealer"), Is.EqualTo("Scholar"));
        Assert.That(MitPlanResolver.ColumnFor("WAR", "OffTank"), Is.EqualTo("OT"));
        Assert.That(MitPlanResolver.ColumnFor("RDM", "CasterDps"), Is.EqualTo("Caster"));
    }

    [Test]
    public void WindowsOpenByTheEffectsDurationAndCloseAtTheHit()
    {
        var items = MitPlanResolver.Resolve(Garuda, CastTimelineTests.Garuda, "MT", "WAR");
        var shriek = items.Single(i => i.Label == "Mistral Shriek");
        var cast = CastTimelineTests.Garuda.Find("Mistral Shriek", 1)!;
        Assert.That(shriek.AcceptIds, Is.EqualTo(new[] { 7535u }));
        Assert.That(shriek.Opens, Is.EqualTo(cast.Start - 15f).Within(0.01f));
        Assert.That(shriek.Closes, Is.EqualTo(cast.Resolve).Within(0.01f));
    }

    [Test]
    public void AfterCastEndOpensAtTheResolve()
    {
        var items = MitPlanResolver.Resolve(Garuda, CastTimelineTests.Garuda, "White Mage", "WHM");
        var asylum = items.Single(i => i.Label == "Mistral Shriek");
        var cast = CastTimelineTests.Garuda.Find("Mistral Shriek", 1)!;
        Assert.That(asylum.Opens, Is.EqualTo(cast.Resolve).Within(0.01f));
    }

    [Test]
    public void ActionsOfOtherJobsDropOut()
    {
        var items = MitPlanResolver.Resolve(Garuda, CastTimelineTests.Garuda, "MT", "PLD");
        var friction2 = items.Single(i => i.Label == "Friction #2");
        Assert.That(friction2.AcceptIds, Is.EqualTo(new[] { 3540u }), "Shake / Veil: a Paladin has Divine Veil only");
    }

    [Test]
    public void UnanchoredMechanicsResolveToNothing()
    {
        var items = MitPlanResolver.Resolve(Garuda, CastTimelineTests.Garuda, "MT", "WAR");
        Assert.That(items.Select(i => i.Label), Does.Not.Contain("Cleanses"));
    }
}
