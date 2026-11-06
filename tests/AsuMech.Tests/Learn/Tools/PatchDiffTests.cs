using AsuMech.Learn.Tools;

namespace AsuMech.Tests;

public class PatchDiffTests
{
    private const string Actions = "#,Name,Cast100ms,CastType,EffectRange,XAxisModifier,Recast100ms,Omen\n0,,0,0,0,0,0,0\n46086,Crown of Arcadia,50,2,60,0,0,12\n46096,Heavy Hitter,0,3,60,0,0,0\n7535,Reprisal,0,2,5,0,600,0\n";
    private const string Statuses = "#,Name,MaxStacks\n0,,0\n1193,Reprisal,0\n297,Galvanize,0\n";

    private static readonly Dictionary<uint, string> ActionIds = new() { [46086] = "M11s.CrownOfArcadia", [46096] = "M11s.HeavyHitter", [7535] = "mit Reprisal" };
    private static readonly Dictionary<uint, string> StatusIds = new() { [1193] = "mit Reprisal", [297] = "mit Adloquium" };

    private static List<PatchChange> Diff(string actionsTo, string statusesTo)
        => PatchDiff.Compare(DataminingSheet.Parse("Action", Actions), DataminingSheet.Parse("Action", actionsTo),
            DataminingSheet.Parse("Status", Statuses), DataminingSheet.Parse("Status", statusesTo), ActionIds, StatusIds);

    [Test]
    public void TheSameSheetsChangeNothing()
    {
        Assert.That(Diff(Actions, Statuses), Is.Empty);
        Assert.That(PatchDiff.Table([]), Does.Contain("no change"));
    }

    [Test]
    public void AnAlteredRowShowsEveryColumnThatMoved()
    {
        var actions = Actions.Replace("46096,Heavy Hitter,0,3,60,0,0,0", "46096,Heavy Hitter,0,3,40,0,0,0").Replace("46086,Crown of Arcadia,50,2", "46086,Crown of Arcadia,47,2");
        var statuses = Statuses.Replace("297,Galvanize,0", "297,Galvanize II,0");
        var changes = Diff(actions, statuses);
        Assert.That(changes.Select(c => (c.Id, c.Column, c.From, c.To)), Is.EqualTo(new[]
        {
            (7535u, "row", "present", "present"),
        }.Where(_ => false).Concat(new[] { (46086u, "cast", "5s", "4.7s"), (46096u, "range", "60", "40"), (297u, "name", "Galvanize", "Galvanize II") })));
        Assert.That(changes.Single(c => c.Id == 46096).UsedBy, Is.EqualTo("M11s.HeavyHitter"));
        Assert.That(PatchDiff.Table(changes), Does.Contain("3 change(s)"));
    }

    [Test]
    public void ARowThatAppearsOrVanishesIsAChange()
    {
        var actions = Actions.Replace("7535,Reprisal,0,2,5,0,600,0\n", "");
        var changes = Diff(actions, Statuses);
        Assert.That(changes.Select(c => (c.Id, c.Column, c.From, c.To)), Is.EqualTo(new[] { (7535u, "row", "present", "missing") }));
    }

    [Test]
    public void TheCatalogNamesItsActionsAndStatuses()
    {
        var actions = PatchDiff.CatalogActions();
        Assert.That(actions, Does.ContainKey(0xB410u));
        Assert.That(actions[0xB410u], Is.EqualTo("M11s.HeavyHitter"));
        Assert.That(actions, Does.ContainKey(7535u));
        Assert.That(actions.Count, Is.GreaterThan(300));
        Assert.That(PatchDiff.MitigationStatuses()[297], Does.Contain("Adloquium"));
    }

    [Test]
    public void SheetsParseQuotedFieldsAndSkipTheHeader()
    {
        var sheet = DataminingSheet.Parse("Status", "#,Name,Description\n0,,\n5,\"Stun, long\",\"says \"\"no\"\"\nand more\"\n6,Plain,x\n");
        Assert.That(sheet.Ids, Is.EquivalentTo(new uint[] { 0, 5, 6 }));
        Assert.That(sheet.Get(5)!.Text("Name"), Is.EqualTo("Stun, long"));
        Assert.That(sheet.Get(5)!.Text("Description"), Is.EqualTo("says \"no\"\nand more"));
        Assert.That(sheet.Get(6)!.Text("Name"), Is.EqualTo("Plain"));
        Assert.That(sheet.Get(7), Is.Null);
    }
}
