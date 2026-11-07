using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Core.Native.Interfaces;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Ucob.P3Blackfire;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; Nael lands north, so the party leads the Liquid Hells
// north and Bahamut dives from the south-west.
public class UcobP3BlackfireScenarioTests
{
    private static NegativeRun<UcobP3BlackfireScenario> NaelNorth(PartyRole role, System.Action<UcobP3BlackfireStateOverrides>? set = null)
        => Negative<UcobP3BlackfireScenario>(role).Overrides<UcobP3BlackfireStateOverrides>(o =>
        {
            o.NaelFrom = Direction.N;
            o.LiquidHellTarget = RegenHealer;
            set?.Invoke(o);
        });

    [Test]
    public void StandingInTheMegaflareDiveKills()
        => NaelNorth(CasterDps)
            .TeleportAt(15f, to: new Vector2(5.66f, -5.66f))
            .ShouldKill(ActionId.MegaflareDive, CasterDps);

    [Test]
    public void StandingInALiquidHellPuddleKills()
        => NaelNorth(CasterDps)
            .TeleportAt(17f, to: new Vector2(0f, -7.2f))
            .ShouldKill(ActionId.LiquidHell, CasterDps);

    [Test]
    public void LeavingYourTowerUnsoakedKillsEveryone()
        => NaelNorth(MeleeDpsA, o => o.StackTargets = [MainTank, RegenHealer, MeleeDpsB, PhysRangedDps])
            .TeleportAt(24f, to: new Vector2(-12f, 0f))
            .ShouldKill(ActionId.MegaflareStrike, AllBut());

    [Test]
    public void MissingTheMegaflareStackKillsYouThenTheShortStack()
        => NaelNorth(MeleeDpsA, o => o.StackTargets = [MainTank, RegenHealer, MeleeDpsA, PhysRangedDps])
            .TeleportAt(22.5f, to: new Vector2(-12f, 0f))
            .ShouldKill(ActionId.MegaflareStack, MeleeDpsA)
            .ShouldKill(ActionId.MegaflareStack, MainTank, RegenHealer, PhysRangedDps);

    [Test]
    public void TheBotsClearWhereverNaelLands([ValueSource(typeof(Direction), nameof(Direction.All))] Direction from)
        => Negative<UcobP3BlackfireScenario>(OffTank)
            .Overrides<UcobP3BlackfireStateOverrides>(o => o.NaelFrom = from)
            .ShouldKillNoOneElse();

    [Test]
    public void TheBotsClearWhoeverLiquidHellChases([ValueSource(nameof(Everyone))] PartyRole target)
        => Negative<UcobP3BlackfireScenario>(PhysRangedDps)
            .Overrides<UcobP3BlackfireStateOverrides>(o => o.LiquidHellTarget = target)
            .ShouldKillNoOneElse();

    [Test]
    public void NaelThrowsEachHypernovaAndLeavesAPuddleWhereItLands()
    {
        var run = ScenarioRun.Execute(typeof(UcobP3BlackfireScenario), 0, 1, new ScenarioRunOptions { PlayerRole = OffTank });
        Assert.That(run.Passed, run.ToString);
        var rows = ((FakeBattleCharas)Natives.BattleCharas).Timeline.Rows;
        var nael = rows.First(r => r["kind"] == "spawn" && r["id"] == BNpcBaseId.NaelDeusDarnus.ToString())["actor"];
        var throws = rows.Where(r => r["kind"] == "ability" && r["id"] == ActionId.Hypernova.ToString()).ToList();
        var puddles = rows.Where(r => r["kind"] == "eobj_spawn" && r["id"] == EObjId.HypernovaPuddle.ToString()).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(throws, Has.Count.EqualTo(UcobP3BlackfireState.HypernovaCount));
            Assert.That(throws.Select(r => r["actor"]), Is.All.EqualTo(nael), "a Hypernova came from someone other than Nael");
            Assert.That(puddles, Has.Count.EqualTo(UcobP3BlackfireState.HypernovaCount));
        });
    }

    private static readonly PartyRole[] Everyone = System.Enum.GetValues<PartyRole>();
}
