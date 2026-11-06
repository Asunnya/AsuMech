using System.Numerics;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Learn.Sources;
using AnoMech.Learn.Tools;

namespace AnoMech.Tests;

public class ScenarioSkeletonTests
{
    private static string Fixture => Path.Combine(AppContext.BaseDirectory, "Learn", "Tools", "Fixtures", "skeleton-pull.log");

    [Test]
    public void PullsFollowTheCombatFlagAndTheClearCommand()
    {
        var pulls = ActLog.Pulls(Fixture, 1325);
        Assert.That(pulls.Select(p => (p.Index, p.Clear, (int)p.Duration)), Is.EqualTo(new[] { (1, false, 26), (2, true, 14) }));
        Assert.That(ActLog.Pulls(Fixture, 1122), Is.Empty, "another territory");
        var pull = pulls[0];
        Assert.That(pull.FirstCastAt, Is.EqualTo(5.17f).Within(0.001f));
        Assert.That(pull.Events.OfType<ActSpawn>().Select(s => s.Actor.Name), Is.EqualTo(new[] { "Trophy Weapon" }), "the zone-in spawns came before combat");
        Assert.That(pull.Events.OfType<ActMarker>().Single().Target.Name, Is.EqualTo("player 1"), "players are numbered, never named");
        Assert.That(pull.Events.OfType<ActAbility>().All(h => h.Target!.Name == "player 1"));
        var casts = pull.Events.OfType<ActCast>().ToList();
        Assert.That(casts.Select(c => (c.Name, c.ActionId, c.CastTime)), Is.EqualTo(new[] { ("Crown of Arcadia", 0xB406u, 4.7f), ("Heavy Hitter", 0xB410u, 2f) }));
        Assert.That(casts[0].Position, Is.EqualTo(new Vector3(100f, 0f, 90f)));
        var hits = pull.Events.OfType<ActAbility>().ToList();
        Assert.That(hits.Select(h => (h.Name, h.Damage)), Is.EqualTo(new[] { ("attack", (int?)6596), ("Crown of Arcadia", 7274), ("Heavy Hitter", 12500) }));
        Assert.That(hits[1].TargetCount, Is.EqualTo(8));
        Assert.That(pull.Events.OfType<ActStatus>().Single().Name, Is.EqualTo("Magic Vulnerability Up"));
        Assert.That(pull.Events.OfType<ActMarker>().Single().MarkerId, Is.EqualTo(0xA1u));
        Assert.That(pull.Events.OfType<ActTether>().Single().TetherId, Is.EqualTo((ushort)0x164));
        Assert.That(pull.Events.OfType<ActDirector>().Select(d => d.Command), Is.EqualTo(new uint[] { 0x40000001, 0x80000027, 0x40000005 }));
        Assert.That(pull.Events.OfType<ActMapEffect>().Single().Index, Is.EqualTo(2));
        Assert.That(pull.Events.OfType<ActDespawn>().Single().Actor.Name, Is.EqualTo("Trophy Weapon"));
    }

    [Test]
    public void ALogWithoutCombatFlagsUsesTheDirector()
    {
        var lines = File.ReadLines(Fixture).Where(l => !l.StartsWith("260|")).ToList();
        var pulls = ActLog.Pulls(lines, 1325);
        Assert.That(pulls, Has.Count.EqualTo(1), "commence to wipe; the second pull has no commence line");
        Assert.That(pulls[0].Events.OfType<ActCast>().Count(), Is.EqualTo(2));
    }

    [Test]
    public void TheSkeletonCarriesTheIdsTheShapesAndThePullsTimeline()
    {
        FakeGame.Install();
        // The whole log is one pull here, commenced before the zone-in spawns, so they are in it.
        var lines = File.ReadLines(Fixture).Where(l => !l.StartsWith("260|")).ToList();
        var commence = lines.Single(l => l.Contains("40000001"));
        lines.Remove(commence);
        lines.Insert(1, commence.Replace("21:30:05.1", "21:30:00.5"));
        var pull = ActLog.Pulls(lines, 1325)[0];
        var files = ScenarioSkeleton.Generate(pull, "M11s", "Opener", new Vector3(100f, 0f, 100f), 0f, Natives.Data, ModuleSource.Load("m11s"));

        Assert.That(files.Constants, Does.Contain("public const byte Level = 100;"));
        Assert.That(files.Constants, Does.Contain("public const uint DirectorEventId = 0x800375CF;"));
        Assert.That(files.Constants, Does.Contain("public const uint TheTyrant = 19180;"));
        Assert.That(files.Constants, Does.Contain("public const uint Helper = 9020;"));
        Assert.That(files.Constants, Does.Contain("public const uint CrownOfArcadia = 0xB406;"));
        Assert.That(files.Constants, Does.Contain("public const uint HeavyHitter = 0xB410;"));
        Assert.That(files.Constants, Does.Contain("public const ushort MagicVulnerabilityUp = 0xB7D;"));
        Assert.That(files.Constants, Does.Contain("MarkerA1 = 0xA1; // TODO: name it (first at 17.5s on player 1)"));
        Assert.That(files.Constants, Does.Contain("Tether164 = 0x164; // TODO"));

        Assert.That(files.Actions, Does.Contain("public static readonly EnemyAction CrownOfArcadia = new(ActionId.CrownOfArcadia)"));
        Assert.That(files.Actions, Does.Contain("Effects = [Damage(), ApplyStatus(StatusId.MagicVulnerabilityUp, 3f)],"));
        Assert.That(files.Actions, Does.Contain("// circle 60y, cast 5s; hit 1 player, 1 for damage; modules: circle 60"));
        Assert.That(files.Actions, Does.Contain("Area = new() { Size = MathF.PI * 0.13f }, // from the public modules (bossmod)"), "Heavy Hitter's cone from the modules, bossmod's 45°");

        Assert.That(files.Scenario, Does.Contain("public sealed class M11sOpenerScenario : IScenario"));
        Assert.That(files.Scenario, Does.Contain("private SimEnemy? theTyrant;"));
        Assert.That(files.Scenario, Does.Contain("private SimEnemy? helper;"));
        Assert.That(files.Scenario, Does.Contain("private SimEnemy? trophyWeapon;"));
        Assert.That(files.Scenario, Does.Contain("world.Events.Add(19.5f, () => trophyWeapon = world.SpawnEnemy("), "a late spawn at its time");
        Assert.That(files.Scenario, Does.Contain("Placement: new Placement(new Vector3(12f, 0f, 0f), 1.57f)"));
        Assert.That(files.Scenario, Does.Contain("world.Events.Add(27.5f, () => trophyWeapon?.Despawn());"));
        Assert.That(files.Scenario, Does.Contain("helper?.SetPosition(new Placement(new Vector3(0f, 0f, -8f), 0f)); helper?.Cast(Actions.HeavyHitter); }"), "a helper is placed where it cast from");
        Assert.That(files.Scenario, Does.Contain("world.Map.AddEffect(0x00020001, 0x02)"));
        Assert.That(files.Scenario, Does.Contain("world.Map.DirectorUpdate(0x80000027, 0x0, 0x2, 0x1BDB, 0x40001A2B)"));
        Assert.That(files.Scenario, Does.Contain("// 17.5s: marker 0xA1 on player 1 -- TODO roll"));
        Assert.That(files.Scenario, Does.Contain("/* TODO roll: on player 1 */"), "the auto-attack's target is the pull's");
        Assert.That(files.Scenario, Does.Not.Contain("Some Player"), "no player name reaches the skeleton");
        var order = files.Scenario.Split('\n').Where(l => l.Contains("world.Events.Add(")).Select(l => float.Parse(l.Split('(')[1].Split('f')[0], System.Globalization.CultureInfo.InvariantCulture)).ToList();
        Assert.That(order, Is.Ordered, "the timeline reads in time order, late spawns included");

        // Drift zero by construction: the generated timeline is the pull's casts and instants.
        var timeline = ScenarioSkeleton.TimelineOf(files.Scenario);
        Assert.That(timeline, Is.EqualTo(new[] { (5f, "Attack"), (9.67f, "CrownOfArcadia"), (21.5f, "HeavyHitter") }));
    }
}
