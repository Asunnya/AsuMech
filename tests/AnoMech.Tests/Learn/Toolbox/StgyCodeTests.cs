using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using AnoMech.Learn.Toolbox;
using AnoMech.Learn.Toolbox.Stgy;
using AnoMech.Scenarios.Umad;
using NUnit.Framework;

namespace AnoMech.Tests.Learn.Toolbox;

public class StgyCodeTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Learn", "Toolbox", "Fixtures", name);
    private static string Code(string name)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Fixture(name)));
        return doc.RootElement.GetProperty("code").GetString()!;
    }

    [Test]
    public void APublicCodeDecodesToItsObjects()
    {
        var doc = StgyCode.Decode(Code("stgy-883.json"));
        Assert.That(doc.Version, Is.EqualTo(2u));
        Assert.That(doc.Name, Is.EqualTo("P4 DEBUFFS/SPOTS"));
        Assert.That(doc.BackgroundName, Is.EqualTo("none"));
        Assert.That(doc.Objects, Has.Count.EqualTo(48));
        var tank1 = doc.Objects.First(o => o.TypeName == "tank_1");
        Assert.That((tank1.X, tank1.Y), Is.EqualTo((134f, 238.5f)));
        var a = doc.Objects.First(o => o.TypeName == "waymark_a");
        Assert.That((a.X, a.Y, a.Locked), Is.EqualTo((134f, 225.5f, true)));
        var line = doc.Objects.First(o => o.TypeName == "line");
        Assert.That((line.X, line.A / 10f, line.C, line.Color.R, line.Color.G, line.Color.B), Is.EqualTo((299.5f, 509f, 6, (byte)255, (byte)128, (byte)0)));
        Assert.That(doc.Objects.Where(o => o.TypeName == "text").Select(o => o.Text), Has.Member("Real       Twister       Donut"));
        var vertical = doc.Objects.First(o => o.TypeName == "line" && o.Angle == 90);
        Assert.That(vertical.B / 10f, Is.EqualTo(384f));
    }

    [Test]
    public void ABrokenCodeIsRefused()
    {
        Assert.Throws<InvalidDataException>(() => StgyCode.Decode("[stgy:aWY!]"));
        Assert.Throws<InvalidDataException>(() => StgyCode.Decode("hello"));
        Assert.That(StgyCode.Looks("paste [stgy:aWYsAM1boKoTECOsYxwIUIynMzdZ31IPOp] here"), Is.True);
    }

    [Test]
    public void BoardsBecomeAToolboxPlanCalibratedByTheirWaymarks()
    {
        var layout = new UmadZone().WaymarkPresets[0];
        var raw = File.ReadAllBytes(Fixture("stgy-883.json"));
        var plan = PlanRef.Parse("https://ffxivstrats.io/fight/dt-ulti-umad?strategy=883");
        Assert.That((plan.Host, plan.Id), Is.EqualTo((RaidplanHost.Stgy, "dt-ulti-umad/883")));
        var (converted, report) = new StgyProvider().Convert(raw, "stgy-883", layout);
        Assert.That(converted.Steps, Has.Count.EqualTo(1));
        Assert.That(converted.Label, Is.EqualTo("P4 DEBUFFS/SPOTS"));
        var step = converted.Steps[0];
        Assert.That(step.Objects.Where(o => o.Kind == "player").Select(o => o.Key), Is.EquivalentTo(new[] { "tank1", "tank2", "healer1", "healer2", "dps1", "dps2", "dps3", "dps4" }));
        Assert.That(step.Objects.Count(o => o.Kind == "waymark"), Is.EqualTo(8));
        Assert.That(report.RoleMap["dps3"], Is.EqualTo("PhysRangedDps"));
        Assert.That(report.Residual, Is.LessThan(1.5f), string.Join("; ", report.Warnings));
        // The author drew the waymark ring 51 board px wide; A sits north of its centre.
        var a = step.Objects.First(o => o.Key == "waymarkA");
        var c = step.Objects.First(o => o.Key == "waymarkC");
        Assert.That(a.Z, Is.LessThan(c.Z));
        Assert.That(step.Objects.Count(o => o.Kind == "tether"), Is.GreaterThanOrEqualTo(4));
        Assert.That(step.Objects.Count(o => o.Kind == "text" && o.Label.Contains("Twister")), Is.EqualTo(2));
    }

    [Test]
    public void SeveralPastedCodesAreSeveralSteps()
    {
        var raw = System.Text.Encoding.UTF8.GetBytes(Code("stgy-883.json") + "\n" + Code("stgy-978.json"));
        var plan = PlanRef.Parse(System.Text.Encoding.UTF8.GetString(raw));
        Assert.That(plan.Host, Is.EqualTo(RaidplanHost.Stgy));
        Assert.That(plan.Id, Does.StartWith("stgy-"));
        var (converted, report) = Raidplans.Convert(plan, raw, new UmadZone().WaymarkPresets[0]);
        Assert.That(converted.Steps.Select(s => s.Label), Is.EqualTo(new[] { "P4 DEBUFFS/SPOTS", "Double Tether DSA" }));
        Assert.That(report.Warnings, Is.Empty.Or.All.Contains("residual"));
    }
}
