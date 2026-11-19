using AsuMech.Core.Game;
using AsuMech.Scenarios;
using AsuMech.Core.Game.Party;
using AsuMech.Core.Native.Interfaces;
using AsuMech.Core.SimObjects;
using AsuMech.Core.UserActions;
using AsuMech.Scenarios.Fru.P5PandorasBox;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Fru.FruConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Pandora's Box goes off at 13.38s; a tank LB3 pressed at 7.76s lands its status on its tank 1.4s on,
// then on each other member 0.124s after the one before, and lasts 8s.
public class FruP5PandorasBoxScenarioTests
{
    private static readonly string[] TankLimitBreaks = ["199", "4240", "4241", "17105"];
    private static readonly string[] TankLimitBreakStatuses = ["196", "863", "864", "1931"];

    [TestCase(MainTank)]
    [TestCase(OffTank)]
    public void TheForcedTankIsTheOneToLimitBreak(PartyRole tank)
        => Assert.That(new FruP5PandorasBoxState(new Rng(1), new FruP5PandorasBoxStateOverrides { LimitBreakTank = tank }).LimitBreakTank, Is.EqualTo(tank));

    [Test]
    public void EitherTankIsRolledToLimitBreak()
    {
        var rolled = Enumerable.Range(0, 64).Select(seed => new FruP5PandorasBoxState(new Rng(seed), new FruP5PandorasBoxStateOverrides()).LimitBreakTank).ToHashSet();
        Assert.That(rolled, Is.EquivalentTo(new[] { MainTank, OffTank }));
    }

    [TestCase(MainTank)]
    [TestCase(OffTank)]
    public void ThePlayerTankWhoNeverPressesLimitBreakWipesTheParty(PartyRole tank)
        => Negative<FruP5PandorasBoxScenario>(tank)
            .Overrides<FruP5PandorasBoxStateOverrides>(o => o.LimitBreakTank = tank)
            .FreezeAt(3f)
            .ShouldKill(ActionId.PandorasBox, PerRole.All);

    // Pressed with the box 0.9s away, no status has landed yet.
    [Test]
    public void ALimitBreakPressedTooLateWipesTheParty()
        => Negative<FruP5PandorasBoxScenario>(MainTank)
            .Overrides<FruP5PandorasBoxStateOverrides>(o => o.LimitBreakTank = MainTank)
            .FreezeAt(3f)
            .At(12.5f, p => PressLimitBreak(p, MainTank))
            .ShouldKill(ActionId.PandorasBox, PerRole.All);

    [TestCase(MainTank)]
    [TestCase(OffTank)]
    public void ThePlayerTanksOwnLimitBreakCarriesTheParty(PartyRole tank)
    {
        var run = ScenarioRun.Execute(typeof(FruP5PandorasBoxScenario), 0, Random.Shared.Next(), new ScenarioRunOptions
        {
            PlayerRole = tank,
            Overrides = o => ((FruP5PandorasBoxStateOverrides)o).LimitBreakTank = tank,
            Takeovers = [new Takeover(3f, null)],
            Probe = p => { if (p.Crossed(9f)) PressLimitBreak(p, tank); },
        });
        Assert.Multiple(() =>
        {
            Assert.That(run.Passed, run.ToString);
            Assert.That(Rows().Count(r => r["kind"] == "status_gain" && TankLimitBreakStatuses.Contains(r["id"])), Is.EqualTo(PerRole.All.Length));
        });
    }

    [TestCase(MainTank, RegenHealer)]
    [TestCase(OffTank, MainTank)]
    public void TheBotTanksLimitBreakCarriesAPlayerWhoFroze(PartyRole tank, PartyRole player)
    {
        string? limitBreakTank = null;
        var run = ScenarioRun.Execute(typeof(FruP5PandorasBoxScenario), 0, Random.Shared.Next(), new ScenarioRunOptions
        {
            PlayerRole = player,
            Overrides = o => ((FruP5PandorasBoxStateOverrides)o).LimitBreakTank = tank,
            Takeovers = [new Takeover(3f, null)],
            Probe = p => limitBreakTank ??= p.Member(tank)!.EntityId.ToString("X8"),
        });
        var limitBreaks = Rows().Where(r => r["kind"] == "ability" && TankLimitBreaks.Contains(r["id"])).ToList();
        var lands = Rows().Where(r => r["kind"] == "status_gain" && TankLimitBreakStatuses.Contains(r["id"])).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(run.Passed, run.ToString);
            Assert.That(limitBreaks, Has.Count.EqualTo(1));
            Assert.That(lands.Select(r => r["actor"]).Distinct().Count(), Is.EqualTo(PerRole.All.Length));
            if (limitBreaks.Count != 1 || lands.Count == 0) return;
            Assert.That(limitBreaks.Single()["actor"], Is.EqualTo(limitBreakTank));
            Assert.That(lands[0]["actor"], Is.EqualTo(limitBreakTank));
            var pressed = Number(limitBreaks.Single()["t"]);
            for (var k = 0; k < lands.Count; k++)
                Assert.That(Number(lands[k]["t"]) - pressed, Is.EqualTo(1.4f + 0.124f * k).Within(0.03f), $"landing {k}");
        });
    }

    private static void PressLimitBreak(ScenarioProbe p, PartyRole role)
    {
        var tank = (SimPlayer)p.Member(role)!;
        var limitBreak = LimitBreakHandler.ActionId(tank.ClassJob, 3);
        JobActions.ApplyEffects(tank, limitBreak, (ulong)tank.GameObjectId, Random.Shared);
        LimitBreakHandler.Resolve(p.Game.World.Party, tank, limitBreak);
    }

    private static IReadOnlyList<Dictionary<string, string>> Rows() => ((FakeBattleCharas)Natives.BattleCharas).Timeline.Rows;

    private static float Number(string text) => float.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
}
