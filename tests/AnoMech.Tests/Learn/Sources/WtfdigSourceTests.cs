using System;
using System.IO;
using System.Linq;
using AnoMech.Learn.Mitigation;
using AnoMech.Learn.Sources;
using NUnit.Framework;

namespace AnoMech.Tests.Learn.Sources;

public class WtfdigSourceTests
{
    private static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Learn", "Sources", "Fixtures", name));

    private static readonly Lazy<WtfdigSource> Umad = new(() =>
        WtfdigSource.Parse(WtfdigSource.Convert("umad", Fixture("wtfdig-umad-data.ts"), Fixture("wtfdig-umad-mits.ts"), "aa793755", "https://github.com/mczub/wtfdig")));

    [Test]
    public void TheRealDataFileConvertsWhole()
    {
        var source = Umad.Value;
        Assert.That(source.StratNames, Is.EqualTo(new[] { "kefkabin", "lpdu" }));
        Assert.That(source.DefaultStrat, Is.EqualTo("kefkabin"));
        Assert.That(source.Toggles("kefkabin")["arrows"], Is.EqualTo("mgr"));
        Assert.That(source.Toggles("lpdu")["blackhole"], Is.EqualTo("lpdu"));
        Assert.That(source.Mits.Select(kv => kv.Key), Is.EquivalentTo(new[] { "ikuya", "lpdu" }));
        Assert.That(source.Config["posterLayout"], Is.Null);
    }

    [Test]
    public void MechanicsResolveForAStratWithRoleTextsBySeat()
    {
        var mechanics = Umad.Value.Mechanics("kefkabin");
        Assert.That(mechanics.Count, Is.EqualTo(82));
        var start = mechanics.First(m => m.Mechanic == "Start");
        Assert.That(start.Action, Is.EqualTo("Supports West, DPS East"));
        Assert.That(start.ByRole["MeleeDpsA"], Is.EqualTo("East"));
        Assert.That(start.ByRole["MainTank"], Is.EqualTo("West"));
        Assert.That(start.ByRole, Has.Count.EqualTo(8));
        Assert.That(Umad.Value.Mechanics("lpdu").Count, Is.EqualTo(80));
        Assert.That(mechanics.Select(m => m.PhaseTag).Distinct(), Has.Member("arrows"));
    }

    [Test]
    public void AMitPlanBecomesTheSheetGridThePlanImporterReads()
    {
        var grid = Umad.Value.MitGrid("ikuya", "p1");
        Assert.That(grid[0], Does.Contain("MT").And.Contain("Caster"));
        var ruin = grid.First(r => r[1] == "Revolting Ruin");
        Assert.That(ruin[grid[0].IndexOf("MT")], Is.EqualTo("Kitchen Sink (3rd hit)"));
        var mystery = grid.First(r => r[1] == "Mystery Magic");
        Assert.That(mystery[grid[0].IndexOf("White Mage")], Does.Contain("Temperance"));
        Assert.That(mystery[grid[0].IndexOf("MT")], Does.Contain("Party Mit"));
        var csv = WtfdigSource.GridToCsv(grid);
        Assert.That(MitSheetImport.ParseCsv(csv).Count, Is.EqualTo(grid.Count));
    }
}
