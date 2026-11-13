using System;
using System.IO;
using System.Linq;
using AsuMech.Core.Game;
using AsuMech.Learn.Toolbox;
using AsuMech.Scenarios.Fru;
using AsuMech.Scenarios.Ucob;
using NUnit.Framework;

namespace AsuMech.Tests.Learn.Toolbox;

public class RaidplanIoProviderTests
{
    private static byte[] Fixture => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Learn", "Toolbox", "Fixtures", "raidplan-PPYgcTqAr4DXZE50.first3.json"));
    private static WaymarkLayout InnerCircle => new FruZone().WaymarkPresets[0];

    [Test]
    public void ADocumentBecomesAToolboxPlanCalibratedByItsWaymarks()
    {
        var plan = PlanRef.Parse("https://raidplan.io/plan/PPYgcTqAr4DXZE50");
        var (converted, report) = Raidplans.Convert(plan, Fixture, InnerCircle);
        Assert.That(converted.Id, Is.EqualTo("PPYgcTqAr4DXZE50"));
        Assert.That(converted.Steps, Has.Count.EqualTo(3));
        Assert.That(report.Residual, Is.LessThan(0.3f), string.Join("; ", report.Warnings));
        // The waymark ring sits ~270 px from the canvas centre; the layout's A-D are 10 yalms out.
        Assert.That(report.Scale, Is.EqualTo(10f / 270f).Within(0.003f));
        var a = converted.Steps[0].Objects.Single(o => o.Key == "waymarkA");
        Assert.That(a.X, Is.EqualTo(0f).Within(0.3f));
        Assert.That(a.Z, Is.EqualTo(-10f).Within(0.3f));
        Assert.That(converted.Steps[0].Objects.Single(o => o.Key == "waymark1").Shape, Is.EqualTo("circle"));
        Assert.That(converted.Steps[0].Objects.Count(o => o.Kind == "waymark"), Is.EqualTo(8));
    }

    [Test]
    public void PlayersMapToTheSharedSlotsWithJobsWhereTheIconNamesOne()
    {
        var (converted, report) = new RaidplanIoProvider().Convert(Fixture, "x", InnerCircle);
        var players = converted.Steps[0].Objects.Where(o => o.Kind == "player").ToDictionary(o => o.Key);
        Assert.That(players.Keys, Is.EquivalentTo(new[] { "tank1", "tank2", "healer1", "healer2", "dps1", "dps2", "dps3", "dps4" }));
        Assert.That(players["tank1"].Label, Is.EqualTo("MT"));
        Assert.That(players["dps4"].Job, Is.EqualTo("RDM"));
        Assert.That(players["healer1"].Job, Is.Null);
        Assert.That(report.RoleMap["tank1"], Is.EqualTo("MainTank"));
        Assert.That(report.RoleMap["dps3"], Is.EqualTo("PhysRangedDps"));
        Assert.That(report.RoleMap, Has.Count.EqualTo(8));
        // Every player lands inside the arena.
        Assert.That(players.Values.All(p => MathF.Sqrt(p.X * p.X + p.Z * p.Z) < 20f), Is.True);
    }

    [Test]
    public void AbilitiesBecomeZonesSizedByTheirScaleAndTextsLabelTheStep()
    {
        var (converted, _) = new RaidplanIoProvider().Convert(Fixture, "x", InnerCircle);
        var quarters = converted.Steps[0].Objects.Where(o => o.Kind == "zone" && o.Label == "ff-quarter").ToList();
        Assert.That(quarters, Has.Count.EqualTo(4));
        Assert.That(quarters[0].Shape, Is.EqualTo("cone"));
        Assert.That(quarters[0].Width, Is.EqualTo(337f * 10f / 270f).Within(0.5f));
        Assert.That(quarters[0].Rotation, Is.EqualTo(202.057f).Within(0.01f));
        Assert.That(converted.Steps[0].Objects.Where(o => o.Kind == "text").Select(o => o.Label), Has.Member("Pairs assigned to WM colours"));
        Assert.That(converted.Steps[0].Label, Is.EqualTo("Pairs assigned to WM colours"));
        Assert.That(converted.Steps[0].Note, Does.StartWith("Mech is solved in color pairs"));
        Assert.That(converted.Steps[1].Objects.Count(o => o.Kind == "zone" && o.Shape == "circle"), Is.GreaterThanOrEqualTo(2));
    }

    // NAUR's UCoB P1 plan draws plain role icons with no MT/H1 text (the Liquid Hell lesson showed no players).
    private static byte[] RoleIcons => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Learn", "Toolbox", "Fixtures", "raidplan-9WEG4AEGVdru3hyF.first2.json"));

    [Test]
    public void UnlabelledRoleIconsTakeTheirGroupsSeats()
    {
        var (converted, report) = new RaidplanIoProvider().Convert(RoleIcons, "x", new UcobZone().WaymarkPresets[0]);
        var all = new[] { "tank1", "tank2", "healer1", "healer2", "dps1", "dps2", "dps3", "dps4" };
        Assert.Multiple(() =>
        {
            foreach (var step in converted.Steps)
                Assert.That(step.Objects.Where(o => o.Kind == "player").Select(o => o.Key), Is.EquivalentTo(all), step.Label);
            Assert.That(converted.Steps[0].Objects.Single(o => o.Key == "tank1").Label, Is.EqualTo("MT"));
            Assert.That(report.RoleMap, Has.Count.EqualTo(8));
        });
    }

    // A seat keeps to the icon nearest where it stood, so a player doesn't swap with its partner between steps.
    [Test]
    public void ASeatFollowsTheNearestIconOnTheNextStep()
    {
        var (converted, _) = new RaidplanIoProvider().Convert(RoleIcons, "x", new UcobZone().WaymarkPresets[0]);
        float Moved(string seat)
        {
            var a = converted.Steps[0].Objects.Single(o => o.Key == seat);
            var b = converted.Steps[1].Objects.Single(o => o.Key == seat);
            return MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));
        }
        float Swapped(string seat, string partner)
        {
            var a = converted.Steps[0].Objects.Single(o => o.Key == seat);
            var b = converted.Steps[1].Objects.Single(o => o.Key == partner);
            return MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));
        }
        Assert.That(Moved("healer1") + Moved("healer2"), Is.LessThanOrEqualTo(Swapped("healer1", "healer2") + Swapped("healer2", "healer1") + 0.01f));
    }
}
