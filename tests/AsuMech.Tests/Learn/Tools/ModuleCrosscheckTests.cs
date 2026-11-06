using AsuMech.Core.Native.Interfaces;
using AsuMech.Learn.Sources;
using AsuMech.Learn.Tools;
using AsuMech.Scenarios.M11s;

namespace AsuMech.Tests;

public class ModuleCrosscheckTests
{
    [Test]
    public void FamilyActionsAreTheActionsClassFields()
    {
        var actions = ModuleCrosscheck.FamilyActions("M11s");
        Assert.That(actions.Select(a => a.Name), Does.Contain("HeavyHitter").And.Contain("CrownOfArcadia"));
        Assert.That(actions.Single(a => a.Name == "HeavyHitter").Action.ActionId, Is.EqualTo(M11sConstants.ActionId.HeavyHitter));
    }

    [Test]
    public void M11sAgainstThePublicModules()
    {
        FakeGame.Install();
        var modules = ModuleSource.Load("m11s")!;
        var rows = ModuleCrosscheck.Check("M11s", modules, Natives.Data, null);
        Assert.That(rows, Has.Count.EqualTo(ModuleCrosscheck.FamilyActions("M11s").Count * 2));
        // Both modules know every action the sim defines.
        Assert.That(rows.Where(r => r.Status == ModuleCrosscheck.Missing).Select(r => $"{r.Source}:{r.Action}"), Is.Empty);
        var differs = rows.Where(r => r.Status == ModuleCrosscheck.Differs).ToList();
        TestContext.Out.WriteLine(string.Join("\n", differs.Select(r => $"{r.Source} {r.Action}: {r.Detail}")));
        // Heavy Hitter: the sim's 45° cone agrees with bossmod's comment; reborn's comment has no angle.
        Assert.That(rows.Where(r => r.Action == "HeavyHitter").Select(r => r.Status), Is.All.EqualTo(ModuleCrosscheck.Ok));
        // Something always disagrees between three readings of one fight; it is reported, not resolved.
        Assert.That(differs, Is.Not.Empty);
        Assert.That(differs.All(r => r.Detail.Contains(" vs ")), "every difference shows both values");
        var summary = ModuleCrosscheck.Summarize("M11s", rows, modules);
        Assert.That(summary.Select(s => s.Reads), Is.EqualTo(new[] { "module bossmod", "module reborn" }));
        Assert.That(summary.All(s => s.Status == Readiness.Degraded), "a disagreement is reported, never resolved");
        Assert.That(ModuleCrosscheck.Summarize("Uwu", [], null).Single().Status, Is.EqualTo(Readiness.Missing));
    }
}
