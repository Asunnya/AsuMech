using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios.Ucob.P1Twintania;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class UcobP1TwintaniaScenarioTests
{
    private static void FirstTwisterOnTheCaster(UcobP1TwintaniaStateOverrides o)
        => o.TwisterTargets = [[CasterDps, MainTank, RegenHealer, MeleeDpsA], [OffTank, ShieldHealer, MeleeDpsB, PhysRangedDps], [MainTank, OffTank, RegenHealer, CasterDps]];

    private static void FirstHatchOnMeleeA(UcobP1TwintaniaStateOverrides o)
        => o.HatchTargets = [[MeleeDpsA], [MeleeDpsB], [MeleeDpsA, PhysRangedDps], [MeleeDpsB, CasterDps], [MeleeDpsA, PhysRangedDps]];

    [Test]
    public void SteppingBackOntoATwisterKillsEveryoneNearby()
        => Negative<UcobP1TwintaniaScenario>(CasterDps)
            .Overrides<UcobP1TwintaniaStateOverrides>(FirstTwisterOnTheCaster)
            .TeleportAt(13.7f, to: new Vector2(6.6f, -4f))
            .ShouldKill(ActionId.TwisterBlast, AllBut(MainTank, OffTank));

    [Test]
    public void TakingAFireballAloneKills()
        => Negative<UcobP1TwintaniaScenario>(CasterDps)
            .Overrides<UcobP1TwintaniaStateOverrides>(o => o.FireballTargets[0] = CasterDps)
            .TeleportAt(15f, to: new Vector2(10f, 5f))
            .ShouldKill(ActionId.Fireball, CasterDps);

    [Test]
    public void StandingInFrontOfPlummetKillsANonTank()
        => Negative<UcobP1TwintaniaScenario>(MeleeDpsA)
            .TeleportAt(6.2f, to: new Vector2(0.1f, -12f))
            .ShouldKill(ActionId.Plummet, MeleeDpsA);

    [Test]
    public void AHatchCaughtOutsideANeurolinkWipes()
        => Negative<UcobP1TwintaniaScenario>(MeleeDpsA)
            .Overrides<UcobP1TwintaniaStateOverrides>(FirstHatchOnMeleeA)
            .TeleportAt(44f, to: new Vector2(-4f, 1.5f))
            .ShouldKill(ActionId.DeepHatch, AllBut());

    [Test]
    public void StandingNearTheHatchOutsideTheNeurolinkKills()
        => Negative<UcobP1TwintaniaScenario>(MeleeDpsB)
            .Overrides<UcobP1TwintaniaStateOverrides>(FirstHatchOnMeleeA)
            .TeleportAt(45.5f, to: new Vector2(4f, -8.5f))
            .ShouldKill(ActionId.Hatch, MeleeDpsB);

    [Test]
    public void StandingInALiquidHellPuddleBurnsToDeath()
        => Negative<UcobP1TwintaniaScenario>(CasterDps)
            .TeleportAt(36f, to: new Vector2(16.5f, -9.5f))
            .ShouldKill(ActionId.LiquidHell, CasterDps);

    [Test]
    public void TheBotsClearWithEveryHatchOnTheBaiters()
        => Negative<UcobP1TwintaniaScenario>(MainTank)
            .Overrides<UcobP1TwintaniaStateOverrides>(o =>
            {
                o.HatchTargets = [[PhysRangedDps], [PhysRangedDps], [PhysRangedDps, CasterDps], [PhysRangedDps, CasterDps], [PhysRangedDps, CasterDps]];
                o.LiquidHellTarget = MeleeDpsB;
            })
            .ShouldKillNoOneElse();

    [Test]
    public void TheBotsClearWhenTheTargetedLiquidHellChasesATank([Values(MainTank, OffTank)] PartyRole tank)
        => Negative<UcobP1TwintaniaScenario>(RegenHealer)
            .Overrides<UcobP1TwintaniaStateOverrides>(o => o.LiquidHellTarget = tank)
            .ShouldKillNoOneElse();

    [Test]
    public void TheTanksTradeTwintaniaAfterEachDeathSentence()
        => Negative<UcobP1TwintaniaScenario>(CasterDps)
            .At(30f, p =>
            {
                Assert.That(p.Member(MainTank)!.HasStatus(StatusId.SlashingResistanceDownII));
                Assert.That(p.Member(OffTank)!.HasStatus(StatusId.SlashingResistanceDownII), Is.False);
            })
            .At(65f, p => Assert.That(p.Member(OffTank)!.HasStatus(StatusId.SlashingResistanceDownII)))
            .ShouldKillNoOneElse();
}
