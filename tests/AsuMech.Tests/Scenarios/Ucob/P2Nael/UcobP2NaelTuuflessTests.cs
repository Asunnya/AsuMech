using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Ucob;
using AsuMech.Scenarios.Ucob.P2Nael;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Tests;

public class UcobP2NaelTuuflessTests
{
    private const int Tuufless = 1;

    [Test]
    public void TuuflessPlaysOnTheElementalMarkers()
    {
        var scenario = new UcobP2NaelScenario();
        Assert.That(UcobMarkers.Of(scenario.AiStrats, Tuufless), Is.SameAs(UcobMarkers.Elemental));
        Assert.That(UcobMarkers.Of(scenario.AiStrats, 0), Is.SameAs(UcobMarkers.Aether));
    }

    // The main tank takes the dive where Nael is held, centred just north, and the party stacks south of her.
    [Test]
    public void NaelIsHeldCentredWithThePartyStackedSouthOfHer([Range(0, 4)] int seed)
    {
        Vector3? nael = null;
        var stack = new List<Vector3>();
        var run = ScenarioRun.Execute(typeof(UcobP2NaelScenario), Tuufless, seed, new ScenarioRunOptions
        {
            StopAt = UcobP2NaelScenario.ThunderMarks[0],
            Probe = p =>
            {
                if (!p.Crossed(UcobP2NaelScenario.ThunderMarks[0] - 0.5f)) return;
                nael = p.World.Children.OfType<SimEnemy>().First(e => e.IsActive && e.BNpcBaseId == BNpcBaseId.NaelDeusDarnus).Position;
                stack.AddRange(Enum.GetValues<PartyRole>().Select(r => p.Member(r)!.Position));
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.Multiple(() =>
        {
            Assert.That(nael!.Value.X, Is.EqualTo(0f).Within(0.5f));
            Assert.That(nael.Value.Z, Is.EqualTo(-3.1f).Within(0.5f));
            Assert.That(stack.Select(at => at.Z - nael.Value.Z), Is.All.GreaterThan(3f));
        });
    }

    // Left to right MT ST H1 H2 D1 D2 D3 D4: the earlier of the first two Thunders goes west of Nael.
    [Test]
    public void TheFirstThundersSplitWestAndEastByPriority([Values(MainTank, OffTank, MeleeDpsB, CasterDps)] PartyRole include)
    {
        var holders = new List<(PartyRole Role, float X)>();
        float? naelX = null;
        var splash = UcobP2NaelScenario.ThunderMarks[0] + UcobP2NaelScenario.ThunderstruckAfterMark + UcobP2NaelScenario.ThunderstruckSeconds;
        var run = ScenarioRun.Execute(typeof(UcobP2NaelScenario), Tuufless, (int)include, new ScenarioRunOptions
        {
            StopAt = splash + 0.5f,
            Overrides = o => ((UcobP2NaelStateOverrides)o).ThunderIncludes[0] = include,
            Probe = p =>
            {
                if (!p.Crossed(splash - 0.2f)) return;
                naelX = p.World.Children.OfType<SimEnemy>().First(e => e.IsActive && e.BNpcBaseId == BNpcBaseId.NaelDeusDarnus).Position.X;
                foreach (var role in Enum.GetValues<PartyRole>())
                    if (p.Member(role) is { } m && m.HasStatus(StatusId.Thunderstruck)) holders.Add((role, m.Position.X));
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(holders, Has.Count.EqualTo(2));
        var ordered = holders.OrderBy(h => h.Role).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(ordered[0].X, Is.LessThan(naelX!.Value - 3f), $"{ordered[0].Role} goes left");
            Assert.That(ordered[1].X, Is.GreaterThan(naelX.Value + 3f), $"{ordered[1].Role} goes right");
        });
    }

    // Raven's Dive spread: tanks in front of Nael, melee behind, ranged out wide.
    [Test]
    public void TheRavenDiveSpreadPutsTheTanksNorthAndTheMeleeBehind()
    {
        var at = new Dictionary<PartyRole, Vector3>();
        Vector3? nael = null;
        var dive = UcobP2NaelScenario.QuoteMoves[2][0];
        var run = ScenarioRun.Execute(typeof(UcobP2NaelScenario), Tuufless, 3, new ScenarioRunOptions
        {
            StopAt = dive + 0.5f,
            Probe = p =>
            {
                if (!p.Crossed(dive - 0.2f)) return;
                nael = p.World.Children.OfType<SimEnemy>().First(e => e.IsActive && e.BNpcBaseId == BNpcBaseId.NaelDeusDarnus).Position;
                foreach (var role in Enum.GetValues<PartyRole>()) at[role] = p.Member(role)!.Position;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.Multiple(() =>
        {
            Assert.That(at[MainTank].Z, Is.LessThan(nael!.Value.Z - 2f));
            Assert.That(at[OffTank].Z, Is.LessThan(nael.Value.Z - 2f));
            Assert.That(at[MainTank].X, Is.LessThan(at[OffTank].X));
            Assert.That(at[MeleeDpsA].Z, Is.GreaterThan(nael.Value.Z + 1f));
            Assert.That(at[MeleeDpsB].Z, Is.GreaterThan(nael.Value.Z + 1f));
            Assert.That(at[MeleeDpsA].X, Is.LessThan(at[MeleeDpsB].X));
            foreach (var a in at)
                foreach (var b in at.Where(b => b.Key > a.Key))
                    Assert.That(Vector2.Distance(new(a.Value.X, a.Value.Z), new(b.Value.X, b.Value.Z)), Is.GreaterThan(UcobP2NaelState.RavenDiveRadius), $"{a.Key}-{b.Key}");
        });
    }

    [Test]
    public void TuuflessClearsEveryQuote([Values(0, 1)] int first, [Values(0, 1)] int second, [Values(0, 1)] int third)
        => NegativeRun.Negative<UcobP2NaelScenario>(PhysRangedDps, Tuufless)
            .Overrides<UcobP2NaelStateOverrides>(o =>
            {
                o.Quotes[0] = first;
                o.Quotes[1] = second;
                o.Quotes[2] = third;
            })
            .ShouldKillNoOneElse();

    [Test]
    public void WithoutThePushTuuflessClearsTheDivebombs([Range(0, 19)] int seed)
    {
        var run = ScenarioRun.Execute(typeof(UcobP2NaelScenario), Tuufless, seed, new ScenarioRunOptions
        {
            Overrides = o => ((UcobP2NaelStateOverrides)o).Push = NaelPush.NoPush,
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void TuuflessClearsEveryRavenDiveTarget([Values] PartyRole target, [Values(0, 1)] int third)
        => NegativeRun.Negative<UcobP2NaelScenario>(PhysRangedDps, Tuufless)
            .Overrides<UcobP2NaelStateOverrides>(o =>
            {
                o.RavenDiveTarget = target;
                o.Quotes[2] = third;
            })
            .Seed((int)target)
            .ShouldKillNoOneElse();
}
