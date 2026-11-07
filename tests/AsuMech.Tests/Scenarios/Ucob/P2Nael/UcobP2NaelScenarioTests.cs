using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Core.Native.Interfaces;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Ucob.P2Nael;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
// Nael lands on the main tank at (0, 9).
public class UcobP2NaelScenarioTests
{
    // The clear's push: after Ravensbeak, Nael stops being a target and the dragons leave.
    [Test]
    public void NaelIsPushedAndTheDragonsLeaveAfterRavensbeak()
    {
        bool? targetableBefore = null, targetableAfter = null;
        int? dragonsBefore = null, dragonsAfter = null;
        var dragonIds = new[] { BNpcBaseId.Firehorn, BNpcBaseId.Iceclaw, BNpcBaseId.Thunderwing, BNpcBaseId.TailOfDarkness, BNpcBaseId.FangOfLight };
        var run = ScenarioRun.Execute(typeof(UcobP2NaelScenario), 0, 1, new ScenarioRunOptions
        {
            StopAt = UcobP2NaelScenario.End - 0.1f,
            Probe = p =>
            {
                SimEnemy? Nael() => p.World.Children.OfType<SimEnemy>().FirstOrDefault(e => e.IsActive && e.BNpcBaseId == BNpcBaseId.NaelDeusDarnus);
                int Dragons() => p.World.Children.OfType<SimEnemy>().Count(e => e.IsActive && dragonIds.Contains(e.BNpcBaseId));
                if (p.Crossed(UcobP2NaelScenario.NaelUntargetable - 0.5f)) { targetableBefore = Nael()?.Targetable; dragonsBefore = Dragons(); }
                if (p.Crossed(UcobP2NaelScenario.DragonsLeave + 0.1f)) { targetableAfter = Nael()?.Targetable; dragonsAfter = Dragons(); }
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.Multiple(() =>
        {
            Assert.That(targetableBefore, Is.True);
            Assert.That(dragonsBefore, Is.EqualTo(5));
            Assert.That(targetableAfter, Is.False);
            Assert.That(dragonsAfter, Is.EqualTo(0));
        });
    }

    // Each dragon's attack lands on the players it targets, so the client plays their hits.
    [Test]
    public void TheDragonsAttacksHitTheirTargets()
    {
        var run = ScenarioRun.Execute(typeof(UcobP2NaelScenario), 0, 2, new ScenarioRunOptions { StopAt = UcobP2NaelScenario.RavensbeakCast });
        Assert.That(run.Passed, run.ToString);
        var rows = ((FakeBattleCharas)Natives.BattleCharas).Timeline.Rows.Where(r => r["kind"] == "ability").ToList();
        var attacks = new (uint Action, string Dragon, int Count)[]
        {
            (ActionId.Iceball, "Iceclaw", UcobP2NaelScenario.IceHits.Count(t => t < UcobP2NaelScenario.RavensbeakCast)),
            (ActionId.ChainLightning, "Thunderwing", UcobP2NaelScenario.ThunderMarks.Length),
            (ActionId.Deathstorm, "tail of darkness", UcobP2NaelScenario.Deathstorms.Length),
            (ActionId.DragonFireball, "Firehorn", UcobP2NaelScenario.FireTethers.Length),
        };
        Assert.Multiple(() =>
        {
            foreach (var (action, dragon, count) in attacks)
            {
                var hits = rows.Where(r => r["id"] == action.ToString()).ToList();
                Assert.That(hits, Has.Count.EqualTo(count), dragon);
                Assert.That(hits.Select(r => r["actor_name"]), Is.All.EqualTo(dragon), dragon);
                Assert.That(hits.Select(r => r["targets"]), Is.All.Not.Empty, dragon);
            }
        });
    }

    // Fang of Light casts each puddle itself; a cleanse circle stays up a while even once it is taken.
    [Test]
    public void FangOfLightCastsThePuddlesAndTheCleansesStayVisible([Values(1, 2, 3)] int seed)
    {
        var run = ScenarioRun.Execute(typeof(UcobP2NaelScenario), 0, seed, new ScenarioRunOptions { StopAt = UcobP2NaelScenario.RavensbeakCast });
        Assert.That(run.Passed, run.ToString);
        var rows = ((FakeBattleCharas)Natives.BattleCharas).Timeline.Rows;
        var casts = rows.Where(r => r["kind"] == "cast" && r["id"] == ActionId.WingsOfSalvation.ToString()).ToList();
        var circles = rows.Where(r => r["kind"] == "eobj_spawn" && r["id"] == EObjId.WingsOfSalvationCleanse.ToString()).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(casts, Has.Count.EqualTo(UcobP2NaelScenario.WingsCasts.Sum(set => set.Length)));
            Assert.That(casts.Select(r => r["actor_name"]), Is.All.EqualTo("fang of light"));
            Assert.That(circles, Has.Count.EqualTo(casts.Count));
            foreach (var circle in circles)
            {
                var gone = rows.FirstOrDefault(r => r["kind"] == "despawn" && r["actor"] == circle["actor"] && Number(r["t"]) >= Number(circle["t"]));
                if (gone != null) Assert.That(Number(gone["t"]) - Number(circle["t"]), Is.GreaterThan(1.5f));
            }
        });
    }

    // The sword of Dalamud in the centre steps down through the opener, all on one arena EObj.
    [Test]
    public void TheCentreSwordSinksThroughTheOpener()
    {
        var run = ScenarioRun.Execute(typeof(UcobP2NaelScenario), 0, 1, new ScenarioRunOptions { StopAt = UcobP2NaelScenario.DalamudDive + 1f });
        Assert.That(run.Passed, run.ToString);
        var beats = ((FakeBattleCharas)Natives.BattleCharas).Timeline.Rows
            .Where(r => r["kind"] == "ac" && r["id"] == "0x19D" && Number(r["t"]) > 5f).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(beats, Has.Count.EqualTo(UcobP2NaelScenario.SwordSinks.Length));
            Assert.That(beats.Select(r => r["actor"]).Distinct().Count(), Is.EqualTo(1));
        });
    }

    // The stack turns a quarter clockwise round Nael for each puddle, so each lands a quarter on from the last.
    [Test]
    public void TheStackTurnsAQuarterClockwisePerPuddle([Range(0, 9)] int seed)
    {
        var casts = UcobP2NaelScenario.WingsCasts.SelectMany((set, s) => set.Select(at => (Set: s, At: at))).ToList();
        var naels = new List<Vector3>();
        var run = ScenarioRun.Execute(typeof(UcobP2NaelScenario), 0, seed, new ScenarioRunOptions
        {
            StopAt = UcobP2NaelScenario.RavensbeakCast,
            Probe = p =>
            {
                if (casts.Any(c => p.Crossed(c.At)))
                    naels.Add(p.World.Children.OfType<SimEnemy>().First(e => e.IsActive && e.BNpcBaseId == BNpcBaseId.NaelDeusDarnus).Position);
            },
        });
        Assert.That(run.Passed, run.ToString);
        var puddles = ((FakeBattleCharas)Natives.BattleCharas).Timeline.Rows
            .Where(r => r["kind"] == "cast" && r["id"] == ActionId.WingsOfSalvation.ToString())
            .Select(r => new Vector2(Number(r["x"]), Number(r["z"]))).ToList();
        Assert.That(puddles, Has.Count.EqualTo(casts.Count));
        var bearings = puddles.Select((at, i) => MathF.Atan2(at.X - naels[i].X, -(at.Y - naels[i].Z)) * 180f / MathF.PI).ToList();
        Assert.Multiple(() =>
        {
            for (var k = 1; k < casts.Count; k++)
            {
                if (casts[k].Set != casts[k - 1].Set) continue;
                var turn = float.Ieee754Remainder(bearings[k] - bearings[k - 1], 360f);
                Assert.That(turn, Is.EqualTo(90f).Within(25f), $"puddle {k}");
            }
        });
    }

    // Each cleanse goes to whoever's Doom is closest to running out.
    [Test]
    public void TheLowestDoomTakesEachCleanse([Range(0, 9)] int seed)
    {
        var wrong = new List<string>();
        var remaining = new Dictionary<PartyRole, float>();
        var run = ScenarioRun.Execute(typeof(UcobP2NaelScenario), 0, seed, new ScenarioRunOptions
        {
            StopAt = UcobP2NaelScenario.RavensbeakCast,
            Overrides = seed % 2 == 0 ? null : o => ((UcobP2NaelStateOverrides)o).DoomTargets =
                [[MeleeDpsB, CasterDps], [ShieldHealer, PhysRangedDps, MeleeDpsA], [RegenHealer, OffTank, CasterDps]],
            Probe = p =>
            {
                var now = new Dictionary<PartyRole, float>();
                foreach (var role in Enum.GetValues<PartyRole>())
                    if (p.Member(role) is { } m && m.IsAlive() && m.FindStatus(StatusId.Doom) is { } doom) now[role] = doom.Remaining;
                foreach (var (role, left) in remaining)
                    if (!now.ContainsKey(role) && left > 0.2f && remaining.Values.Min() < left - 0.01f)
                        wrong.Add($"{role} took a cleanse at {p.Time:F2} with {left:F2}s left while another had {remaining.Values.Min():F2}s");
                remaining.Clear();
                foreach (var (role, left) in now) remaining[role] = left;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(wrong, Is.Empty);
    }

    private static float Number(string s) => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

    [Test]
    public void StandingInAThermionicBurstSliceKills()
        => Negative<UcobP2NaelScenario>(CasterDps)
            .TeleportAt(15f, to: new Vector2(0f, 12f))
            .ShouldKill(ActionId.ThermionicBurst, CasterDps);

    [Test]
    public void TwoMeteorStreamsOnOneSpotKillBoth()
        => Negative<UcobP2NaelScenario>(MeleeDpsA)
            .Overrides<UcobP2NaelStateOverrides>(o => o.MeteorOrder = [MainTank, MeleeDpsA, RegenHealer, ShieldHealer, OffTank, MeleeDpsB, PhysRangedDps, CasterDps])
            .TeleportAt(13.4f, to: new Vector2(4.6f, 11.1f))
            .ShouldKill(ActionId.MeteorStream, MeleeDpsA, MainTank);

    [Test]
    public void StandingBesideTheTankForDalamudDiveKills()
        => Negative<UcobP2NaelScenario>(MeleeDpsA)
            .TeleportAt(19.7f, to: new Vector2(0f, 10.5f))
            .ShouldKill(ActionId.DalamudDive, MeleeDpsA);

    [Test]
    public void StandingNextToAThunderstruckPlayerKills()
        => Negative<UcobP2NaelScenario>(CasterDps)
            .Overrides<UcobP2NaelStateOverrides>(o => o.ThunderTargets = [[MeleeDpsA, MeleeDpsB], [MeleeDpsA, MeleeDpsB], [RegenHealer, ShieldHealer], [RegenHealer, ShieldHealer]])
            .TeleportAt(44f, to: new Vector2(-5.2f, 9f))
            .ShouldKill(ActionId.ChainLightningSplash, CasterDps);

    [Test]
    public void StayingAwayForLunarDynamoKills()
        => Negative<UcobP2NaelScenario>(CasterDps)
            .Overrides<UcobP2NaelStateOverrides>(o => o.Quotes[0] = 0)
            .TeleportAt(43.5f, to: new Vector2(0f, -5f))
            .ShouldKill(ActionId.LunarDynamo, CasterDps);

    [Test]
    public void StayingInForIronChariotKills()
        => Negative<UcobP2NaelScenario>(CasterDps)
            .Overrides<UcobP2NaelStateOverrides>(o => o.Quotes[0] = 1)
            .TeleportAt(47f, to: new Vector2(0f, 6f))
            .ShouldKill(ActionId.IronChariot, CasterDps);

    [Test]
    public void AnUncleansedDoomKills()
        => Negative<UcobP2NaelScenario>(CasterDps)
            .Overrides<UcobP2NaelStateOverrides>(o => o.DoomTargets = [[CasterDps, MeleeDpsA], [RegenHealer, ShieldHealer, MeleeDpsB], [OffTank, MainTank, PhysRangedDps]])
            .TeleportAt(50f, to: new Vector2(-10f, -5f))
            .ShouldKill(ActionId.Deathstorm, CasterDps);

    [Test]
    public void TheBotsClearWhenTheSameTwoTakeEveryFire()
        => Negative<UcobP2NaelScenario>(RegenHealer)
            .Overrides<UcobP2NaelStateOverrides>(o => o.FireTargets = [MainTank, MainTank, OffTank, OffTank])
            .ShouldKillNoOneElse();

    [Test]
    public void TheBotsClearEveryQuote([Values(0, 1)] int first, [Values(0, 1)] int second, [Values(0, 1)] int third)
        => Negative<UcobP2NaelScenario>(PhysRangedDps)
            .Overrides<UcobP2NaelStateOverrides>(o =>
            {
                o.Quotes[0] = first;
                o.Quotes[1] = second;
                o.Quotes[2] = third;
            })
            .ShouldKillNoOneElse();

    private static readonly int[][] EveryDragonLayout = Combinations(Enumerable.Range(0, 8).ToArray(), 5).ToArray();

    private static IEnumerable<int[]> Combinations(int[] from, int count)
        => count == 0 ? [[]] : from.SelectMany((first, i) => Combinations(from[(i + 1)..], count - 1).Select(rest => rest.Prepend(first).ToArray()));

    [Test]
    public void WithoutThePushTheBotsClearTheDivebombs([Range(0, 19)] int seed)
    {
        var run = ScenarioRun.Execute(typeof(UcobP2NaelScenario), 0, seed, new ScenarioRunOptions
        {
            Overrides = o => ((UcobP2NaelStateOverrides)o).Push = NaelPush.NoPush,
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void WithoutThePushTheBotsClearEveryDragonLayout([Range(0, 55)] int layout)
        => Negative<UcobP2NaelScenario>(PhysRangedDps)
            .Overrides<UcobP2NaelStateOverrides>(o =>
            {
                o.Push = NaelPush.NoPush;
                o.DragonSlots = EveryDragonLayout[layout];
                o.DivebombQuote = layout % 2;
            })
            .Seed(layout)
            .ShouldKillNoOneElse();

    [Test]
    public void WithoutThePushTheDragonsDiveAndNaelComesBackForTheTank()
    {
        int? dragonsAfter = null;
        bool? naelTargetableAfterLeaving = null, naelTargetableAfterDive = null;
        var dragonIds = new[] { BNpcBaseId.Firehorn, BNpcBaseId.Iceclaw, BNpcBaseId.Thunderwing, BNpcBaseId.TailOfDarkness, BNpcBaseId.FangOfLight };
        var run = ScenarioRun.Execute(typeof(UcobP2NaelScenario), 0, 3, new ScenarioRunOptions
        {
            Overrides = o => ((UcobP2NaelStateOverrides)o).Push = NaelPush.NoPush,
            Probe = p =>
            {
                SimEnemy? Nael() => p.World.Children.OfType<SimEnemy>().FirstOrDefault(e => e.IsActive && e.BNpcBaseId == BNpcBaseId.NaelDeusDarnus);
                if (p.Crossed(UcobP2NaelScenario.NaelLeavesForTheDive + 1f)) naelTargetableAfterLeaving = Nael()?.Targetable;
                if (p.Crossed(UcobP2NaelScenario.DivebombQuoteMoves[1] + 0.5f)) naelTargetableAfterDive = Nael()?.Targetable;
                if (p.Crossed(UcobP2NaelScenario.DivebombLocks[^1] + UcobP2NaelScenario.CauterizeCastSeconds + 3.5f))
                    dragonsAfter = p.World.Children.OfType<SimEnemy>().Count(e => e.IsActive && dragonIds.Contains(e.BNpcBaseId));
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.Multiple(() =>
        {
            Assert.That(naelTargetableAfterLeaving, Is.False);
            Assert.That(naelTargetableAfterDive, Is.True);
            Assert.That(dragonsAfter, Is.EqualTo(0));
        });
    }

    // Dragons north, north-east, south, west and north-west: the first bait stands north-north-east.
    private static void NorthLayout(UcobP2NaelStateOverrides o)
    {
        o.Push = NaelPush.NoPush;
        o.DragonSlots = [0, 1, 4, 6, 7];
        o.DivebombPicks[0] = MeleeDpsA;
        o.DivebombPicks[1] = MeleeDpsB;
        o.DivebombPicks[2] = RegenHealer;
    }

    [Test]
    public void StandingInADragonsPathKills()
        => Negative<UcobP2NaelScenario>(CasterDps)
            .Overrides<UcobP2NaelStateOverrides>(NorthLayout)
            .TeleportAt(149.5f, to: new Vector2(2.6f, -22.5f))
            .ShouldKill(ActionId.CauterizeFirehorn, CasterDps);

    [Test]
    public void TheLastBaitStayingInTheDragonsPathDies()
        => Negative<UcobP2NaelScenario>(RegenHealer)
            .Overrides<UcobP2NaelStateOverrides>(o =>
            {
                NorthLayout(o);
                o.DivebombStackTarget = MeleeDpsA;
            })
            .FreezeAt(154.9f)
            .ShouldKill(ActionId.CauterizeTailOfDarkness, RegenHealer);

    [Test]
    public void StandingStillWhereAHypernovaLandsKills()
        => Negative<UcobP2NaelScenario>(MeleeDpsA)
            .Overrides<UcobP2NaelStateOverrides>(NorthLayout)
            .FreezeAt(142.5f)
            .ShouldKill(ActionId.Hypernova, MeleeDpsA);

    [Test]
    public void StandingNearTheTankForNaelsDiveKills()
        => Negative<UcobP2NaelScenario>(MainTank)
            .Overrides<UcobP2NaelStateOverrides>(o =>
            {
                NorthLayout(o);
                o.DivebombQuote = 1;
            })
            .TeleportAt(153.7f, to: Vector2.Zero)
            .ShouldKill(ActionId.DalamudDive, AllBut(MainTank, OffTank, RegenHealer));
}
