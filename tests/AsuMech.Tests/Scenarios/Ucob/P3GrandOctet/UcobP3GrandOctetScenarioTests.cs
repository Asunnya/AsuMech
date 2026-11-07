using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios.Ucob.P3GrandOctet;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
// Strat 0 tank LB3s the stacks, strat 1 stacks them clockwise of Twintania.
public class UcobP3GrandOctetScenarioTests
{
    private const int TankLimitBreak = 0;
    private const int NoLimitBreak = 1;

    // Bahamut east, Nael northwest, Twintania south; supports take the stacks.
    private static void SupportsStack(UcobP3GrandOctetStateOverrides o)
    {
        o.Slots = [2, 7, 4, 1, 0, 6, 5, 3];
        o.StackTargets = [MainTank, OffTank, RegenHealer, ShieldHealer];
        o.TwintaniaTarget = MeleeDpsA;
    }

    [Test]
    public void StayingInTheCentreForLunarDiveKills()
        => Negative<UcobP3GrandOctetScenario>(CasterDps)
            .Overrides<UcobP3GrandOctetStateOverrides>(SupportsStack)
            .FreezeAt(17f)
            .ShouldKill(ActionId.LunarDive, CasterDps)
            .ShouldKillNoOneElse();

    [Test]
    public void AnUnsoakedMegaflareTowerWipes()
        => Negative<UcobP3GrandOctetScenario>(MeleeDpsB, NoLimitBreak)
            .Overrides<UcobP3GrandOctetStateOverrides>(o =>
            {
                SupportsStack(o);
                o.TwisterTargets = [MainTank, OffTank, RegenHealer, ShieldHealer];
            })
            .TeleportAt(41.5f, to: new Vector2(3f, -3f))
            .ShouldKill(ActionId.MegaflareStrike, AllBut());

    [Test]
    public void TakingAMegaflareStackAloneWithoutLimitBreakKills()
        => Negative<UcobP3GrandOctetScenario>(MainTank, NoLimitBreak)
            .Overrides<UcobP3GrandOctetStateOverrides>(SupportsStack)
            .TeleportAt(44f, to: new Vector2(4f, -4f))
            .ShouldKill(ActionId.MegaflareStack, MainTank)
            .ShouldKillNoOneElse();

    [Test]
    public void TouchingATwisterLaunchesEveryoneNearIt()
        => Negative<UcobP3GrandOctetScenario>(RegenHealer, NoLimitBreak)
            .Overrides<UcobP3GrandOctetStateOverrides>(o =>
            {
                SupportsStack(o);
                o.TwisterTargets = [RegenHealer, MeleeDpsB, PhysRangedDps, CasterDps];
            })
            .FreezeAt(47.6f)
            .ShouldKillSomeone(ActionId.TwisterBlast);

    [Test]
    public void TheTankLimitBreakCoversTheStacksWhereverTheyStand()
    {
        var limitBreakUp = false;
        Negative<UcobP3GrandOctetScenario>(PhysRangedDps, TankLimitBreak)
            .Overrides<UcobP3GrandOctetStateOverrides>(SupportsStack)
            .WithMitigationChecks()
            .At(UcobP3GrandOctetScenario.Stacks - 0.1f, p => limitBreakUp = p.World.Party.Get(ShieldHealer)?.HasStatus(StatusId.LandWaker) == true)
            .ShouldKillNoOneElse();
        Assert.That(limitBreakUp, Is.True);
    }

    [Test]
    public void TheBotsClearWhereverBahamutLands([Range(0, 7)] int slot, [Values(TankLimitBreak, NoLimitBreak)] int strat)
        => Negative<UcobP3GrandOctetScenario>(OffTank, strat)
            .Overrides<UcobP3GrandOctetStateOverrides>(o => o.BahamutSlot = slot)
            .ShouldKillNoOneElse();
}
