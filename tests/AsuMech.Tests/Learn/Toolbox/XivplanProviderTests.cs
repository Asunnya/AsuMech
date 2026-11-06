using System.Linq;
using AsuMech.Learn.Toolbox;
using AsuMech.Scenarios.Fru;
using NUnit.Framework;

namespace AsuMech.Tests.Learn.Toolbox;

public class XivplanProviderTests
{
    private const string Issue109 = "https://xivplan.netlify.app/#/plan/eJzNkkFPgzAUx-_7FASv6FaYxO3m0U_gwSzmra1QHS2WLowsfHdfC3SrM4uJFy-E9_uVPt6_Pc6iKJb8YJ5YvI6yxJaguQSsjlhg2ZRQcyxjzakBWex4nAymFcyUaPLFYiQlF0VpAlQDY0IWyEg6sUIL5htgbbqfO6DSqm1QLT2garevpGMO9fjs3X83hteWvzjuN1fbd9z2JM5l0L0C_cG1b-2chMq5Z-isjh5DLSoonJ8PH8_bYd0r3NU4c7DW50iFpmcjTlMpbeXNG8m2eR5aVQMVprMZTrmOZjqCZRpgfw7fuFYGjFASTbjPAcntKlxs-90_hPPaYyOe9MnfEyW_TJRcSbT53OOl_YeJptlFojm5TDQ9JTq-bfzd3sz6L3N5zdQ";

    [Test]
    public void ARealLinkDecodesToItsScene()
    {
        var json = XivplanProvider.Decode(Issue109);
        Assert.That(json, Does.Contain("\"nextId\": 3").And.Contain("waymark_a"));
        Assert.That(XivplanProvider.Decode(XivplanProvider.Encode(json)), Is.EqualTo(json));
        Assert.That(PlanRef.Parse(Issue109).Host, Is.EqualTo(RaidplanHost.Xivplan));
    }

    [Test]
    public void ASceneBecomesAPlanCalibratedByItsWaymarks()
    {
        // Arena 600 wide; A north, B east, C south, D west at 250 px, as the FRU inner circle at 10 yalms would be.
        const string scene = """
            { "nextId": 20, "arena": { "shape": "circle", "width": 600, "height": 600, "padding": 120, "grid": { "type": "none" } },
              "steps": [ { "objects": [
                { "type": "party", "name": "Tank 1", "image": "/actor/tank1.png", "x": 0, "y": 100, "id": 1, "opacity": 100 },
                { "type": "party", "name": "Paladin", "image": "/actor/PLD.png", "x": 0, "y": -100, "id": 2, "opacity": 100 },
                { "type": "party", "name": "Scholar", "image": "/actor/SCH.png", "x": 50, "y": 0, "id": 3, "opacity": 100 },
                { "type": "party", "name": "Bard", "image": "/actor/BRD.png", "x": -50, "y": 0, "id": 4, "opacity": 100 },
                { "type": "marker", "name": "Waymark A", "image": "/marker/waymark_a.png", "shape": "circle", "x": 0, "y": 250, "id": 5 },
                { "type": "marker", "name": "Waymark B", "image": "/marker/waymark_b.png", "shape": "circle", "x": 250, "y": 0, "id": 6 },
                { "type": "marker", "name": "Waymark C", "image": "/marker/waymark_c.png", "shape": "circle", "x": 0, "y": -250, "id": 7 },
                { "type": "marker", "name": "Waymark D", "image": "/marker/waymark_d.png", "shape": "circle", "x": -250, "y": 0, "id": 8 },
                { "type": "enemy", "name": "Boss", "x": 0, "y": 0, "radius": 50, "rotation": 0, "color": "#ff0000", "id": 9 },
                { "type": "circle", "x": 100, "y": 100, "radius": 75, "color": "#fc972b", "id": 10 },
                { "type": "line", "x": 0, "y": 0, "rotation": 90, "length": 250, "width": 50, "color": "#fc972b", "id": 11 },
                { "type": "cone", "x": 0, "y": 0, "rotation": 180, "radius": 150, "coneAngle": 60, "id": 12 },
                { "type": "text", "text": "Hello", "x": -200, "y": 200, "id": 13 },
                { "type": "tether", "tether": "line", "startId": 1, "endId": 2, "id": 14 },
                { "type": "party", "name": "Hidden", "image": "/actor/WHM.png", "x": 0, "y": 0, "id": 15, "hide": true },
                { "type": "icon", "name": "Attack 1", "image": "/marker/attack1.png", "x": 10, "y": 10, "id": 16, "positionParentId": 2 }
              ] } ] }
            """;
        var (plan, report) = new XivplanProvider().Convert(System.Text.Encoding.UTF8.GetBytes(XivplanProvider.Encode(scene)), "x", new FruZone().WaymarkPresets[0]);
        Assert.That(report.Warnings, Is.Empty);
        Assert.That(report.Scale, Is.EqualTo(10f / 250f).Within(1e-4f));
        var step = plan.Steps.Single();
        var tank1 = step.Objects.Single(o => o.Key == "tank1");
        Assert.That(tank1.X, Is.EqualTo(0f).Within(1e-4f));
        Assert.That(tank1.Z, Is.EqualTo(-4f).Within(1e-4f));
        Assert.That(step.Objects.Single(o => o.Key == "tank2").Job, Is.EqualTo("PLD"));
        Assert.That(step.Objects.Single(o => o.Key == "healer2").Job, Is.EqualTo("SCH"));
        Assert.That(report.RoleMap["healer2"], Is.EqualTo("ShieldHealer"));
        Assert.That(step.Objects.Single(o => o.Key == "dps3").Job, Is.EqualTo("BRD"));
        Assert.That(step.Objects.Count(o => o.Kind == "player"), Is.EqualTo(4));
        Assert.That(step.Objects.Single(o => o.Kind == "boss").Width, Is.EqualTo(4f).Within(1e-3f));
        var circle = step.Objects.Single(o => o.Kind == "zone" && o.Label == "circle");
        Assert.That(circle.X, Is.EqualTo(4f).Within(1e-4f));
        Assert.That(circle.Z, Is.EqualTo(-4f).Within(1e-4f));
        Assert.That(circle.Width, Is.EqualTo(6f).Within(1e-4f));
        var line = step.Objects.Single(o => o.Kind == "zone" && o.Label == "line");
        Assert.That(line.X, Is.EqualTo(5f).Within(1e-3f));
        Assert.That(line.Z, Is.EqualTo(0f).Within(1e-3f));
        Assert.That(step.Objects.Single(o => o.Kind == "zone" && o.Shape == "cone").Label, Is.EqualTo("cone 60°"));
        var tether = step.Objects.Single(o => o.Kind == "tether");
        Assert.That(tether.Height, Is.EqualTo(8f).Within(1e-3f));
        var icon = step.Objects.Single(o => o.Kind == "text" && o.Label == "Attack 1");
        Assert.That(icon.X, Is.EqualTo(0.4f).Within(1e-4f));
        Assert.That(icon.Z, Is.EqualTo(3.6f).Within(1e-4f));
        Assert.That(step.Objects.Any(o => o.Label == "Hidden"), Is.False);
    }
}
