using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Ucob.P4Adds;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
// The bots hold Twintania around (11, -8.5) and Nael around (15, -3).
public class UcobP4AddsScenarioTests
{
    private static void MeleeAndRangedHatchFirst(UcobP4AddsStateOverrides o)
        => o.HatchTargets = [[MeleeDpsA, MeleeDpsB, PhysRangedDps], [MeleeDpsA, MeleeDpsB, CasterDps]];

    [Test]
    public void StandingInFrontOfPlummetKillsANonTank()
        => Negative<UcobP4AddsScenario>(MeleeDpsA)
            .TeleportAt(18.5f, to: new Vector2(13.5f, -8f))
            .ShouldKill(ActionId.Plummet, MeleeDpsA);

    [Test]
    public void StandingInALiquidHellPuddleBurnsToDeath()
        => Negative<UcobP4AddsScenario>(PhysRangedDps)
            .FreezeAt(22.4f)
            .ShouldKill(ActionId.LiquidHell, PhysRangedDps);

    [Test]
    public void AHatchCaughtOutsideANeurolinkWipes()
        => Negative<UcobP4AddsScenario>(MeleeDpsA)
            .Overrides<UcobP4AddsStateOverrides>(MeleeAndRangedHatchFirst)
            .TeleportAt(29f, to: new Vector2(0.1f, -3f))
            .ShouldKill(ActionId.DeepHatch, AllBut());

    [Test]
    public void StandingNearAHatchOutsideANeurolinkKills()
        => Negative<UcobP4AddsScenario>(CasterDps)
            .Overrides<UcobP4AddsStateOverrides>(MeleeAndRangedHatchFirst)
            .TeleportAt(30f, to: new Vector2(3f, -6f))
            .ShouldKill(ActionId.Hatch, CasterDps);

    [Test]
    public void StandingOnYourTwisterKillsYou()
        => Negative<UcobP4AddsScenario>(CasterDps)
            .Overrides<UcobP4AddsStateOverrides>(o =>
            {
                MeleeAndRangedHatchFirst(o);
                o.TwisterTargets = [[CasterDps, MainTank, RegenHealer, MeleeDpsA], [OffTank, ShieldHealer, MeleeDpsB, PhysRangedDps], [MainTank, OffTank, RegenHealer, CasterDps], [ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps]];
            })
            .TeleportAt(35f, to: new Vector2(-15f, -5f))
            .ShouldKill(ActionId.TwisterBlast, CasterDps);

    [Test]
    public void StayingAwayForLunarDynamoKills()
        => Negative<UcobP4AddsScenario>(CasterDps)
            .Overrides<UcobP4AddsStateOverrides>(o => o.FirstQuote = 0)
            .TeleportAt(42.5f, to: new Vector2(-10f, 0f))
            .ShouldKill(ActionId.LunarDynamo, CasterDps);

    [Test]
    public void StayingInForIronChariotKills()
        => Negative<UcobP4AddsScenario>(CasterDps)
            .Overrides<UcobP4AddsStateOverrides>(o => o.FirstQuote = 2)
            .TeleportAt(42.8f, to: new Vector2(12f, -3f))
            .ShouldKill(ActionId.IronChariot, CasterDps);

    [Test]
    public void TakingAThermionicBeamAloneKills()
        => Negative<UcobP4AddsScenario>(CasterDps)
            .Overrides<UcobP4AddsStateOverrides>(o =>
            {
                o.FirstQuote = 2;
                o.BeamTargets[0] = CasterDps;
            })
            .TeleportAt(45.5f, to: new Vector2(-12f, -8f))
            .ShouldKill(ActionId.ThermionicBeam, CasterDps);

    [Test]
    public void TheTanksTradeTheAddsAfterTheirBusters([Values(MainTank, OffTank)] PartyRole twintaniaTank)
    {
        var naelTank = twintaniaTank == MainTank ? OffTank : MainTank;
        Negative<UcobP4AddsScenario>(CasterDps)
            .Overrides<UcobP4AddsStateOverrides>(o => o.TwintaniaTank = twintaniaTank)
            .At(UcobP4AddsScenario.AggroSwap, p => Assert.Multiple(() =>
            {
                Assert.That(p.Member(twintaniaTank)!.HasStatus(StatusId.SlashingResistanceDownII));
                Assert.That(p.Member(naelTank)!.HasStatus(StatusId.PiercingResistanceDownII));
                Assert.That(p.Member(twintaniaTank)!.HasStatus(StatusId.PiercingResistanceDownII), Is.False);
            }))
            .ShouldKillNoOneElse();
    }

    [Test]
    public void TheAddsDieWhenTheClearKilledThem()
    {
        bool? twintaniaDeadBefore = null, twintaniaDeadAfter = null, naelDeadBeforeHerTime = null, naelDeadAfter = null;
        var run = ScenarioRun.Execute(typeof(UcobP4AddsScenario), 0, 1, new ScenarioRunOptions
        {
            StopAt = UcobP4AddsScenario.NaelDies + 0.5f,
            Probe = p =>
            {
                SimEnemy? Add(uint baseId) => p.World.Children.OfType<SimEnemy>().FirstOrDefault(e => e.IsActive && e.BNpcBaseId == baseId);
                if (p.Crossed(UcobP4AddsScenario.TwintaniaDies - 0.2f)) twintaniaDeadBefore = Add(BNpcBaseId.Twintania)?.Dead;
                if (p.Crossed(UcobP4AddsScenario.TwintaniaDies + 0.2f))
                {
                    twintaniaDeadAfter = Add(BNpcBaseId.Twintania)?.Dead;
                    naelDeadBeforeHerTime = Add(BNpcBaseId.NaelDeusDarnus)?.Dead;
                }
                if (p.Crossed(UcobP4AddsScenario.NaelDies + 0.2f)) naelDeadAfter = Add(BNpcBaseId.NaelDeusDarnus)?.Dead;
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.Multiple(() =>
        {
            Assert.That(twintaniaDeadBefore, Is.False);
            Assert.That(twintaniaDeadAfter, Is.True);
            Assert.That(naelDeadBeforeHerTime, Is.False);
            Assert.That(naelDeadAfter, Is.True);
        });
    }

    [Test]
    public void TheBotsClearEveryQuotePair([Range(0, 3)] int first, [Values(0, 1)] int second)
        => Negative<UcobP4AddsScenario>(RegenHealer)
            .Overrides<UcobP4AddsStateOverrides>(o =>
            {
                o.FirstQuote = first;
                o.SecondQuote = second;
            })
            .ShouldKillNoOneElse();

    [Test]
    public void TheBotsClearWhoeverTheGeneratesMark([Values(MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps)] PartyRole spared)
        => Negative<UcobP4AddsScenario>(MainTank)
            .Overrides<UcobP4AddsStateOverrides>(o =>
            {
                var marked = new[] { MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps }.Where(r => r != spared).ToList();
                o.HatchTargets = [marked, marked];
            })
            .ShouldKillNoOneElse();

    [Test]
    public void TheBotsClearWhoeverNaelDivesOn([Values] PartyRole role)
        => Negative<UcobP4AddsScenario>(ShieldHealer)
            .Overrides<UcobP4AddsStateOverrides>(o =>
            {
                o.FirstQuote = 0;
                o.RavenDiveTargets[0] = role;
                o.RavenDiveTargets[1] = role;
            })
            .ShouldKillNoOneElse();

    [Test]
    public void TheBotsClearWithOffTankOnTwintania()
        => Negative<UcobP4AddsScenario>(PhysRangedDps)
            .Overrides<UcobP4AddsStateOverrides>(o => o.TwintaniaTank = OffTank)
            .ShouldKillNoOneElse();
}
