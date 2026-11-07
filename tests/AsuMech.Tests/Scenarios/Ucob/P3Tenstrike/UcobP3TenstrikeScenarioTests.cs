using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios.Ucob.P3Tenstrike;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over. With
// these Hatch targets the caster takes Neurolink 1, north.
public class UcobP3TenstrikeScenarioTests
{
    private static void HealersAndCasterHatch(UcobP3TenstrikeStateOverrides o) => o.HatchTargets = [RegenHealer, ShieldHealer, CasterDps];

    private static void TanksAndHealersShakeFirst(UcobP3TenstrikeStateOverrides o) => o.FirstEarthShakers = [MainTank, OffTank, RegenHealer, ShieldHealer];

    [Test]
    public void AnOviformReachingSomeoneOutsideANeurolinkWipes()
        => Negative<UcobP3TenstrikeScenario>(CasterDps)
            .Overrides<UcobP3TenstrikeStateOverrides>(HealersAndCasterHatch)
            .TeleportAt(15f, to: new Vector2(0f, -4.5f))
            .ShouldKill(ActionId.DeepHatch, AllBut());

    [Test]
    public void StayingInTheNeurolinkForTheSecondHatchKills()
        => Negative<UcobP3TenstrikeScenario>(CasterDps)
            .Overrides<UcobP3TenstrikeStateOverrides>(o =>
            {
                HealersAndCasterHatch(o);
                o.MeteorOrder = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps, RegenHealer, ShieldHealer, OffTank, MainTank];
            })
            .FreezeAt(19.5f)
            .ShouldKill(ActionId.Hatch, CasterDps)
            .ShouldKillNoOneElse();

    [Test]
    public void StandingOnSomeoneElsesMeteorStreamKillsThem()
        => Negative<UcobP3TenstrikeScenario>(PhysRangedDps)
            .Overrides<UcobP3TenstrikeStateOverrides>(o =>
            {
                HealersAndCasterHatch(o);
                o.MeteorOrder = [PhysRangedDps, MeleeDpsA, MeleeDpsB, CasterDps, RegenHealer, ShieldHealer, OffTank, MainTank];
            })
            .TeleportAt(13.5f, to: new Vector2(-5f, -14f))
            .MoveBotAt(13.5f, MeleeDpsA, to: new Vector2(-3.5f, -14f))
            .ShouldKill(ActionId.MeteorStream, MeleeDpsA);

    [Test]
    public void ASecondEarthShakerKills()
        => Negative<UcobP3TenstrikeScenario>(RegenHealer)
            .Overrides<UcobP3TenstrikeStateOverrides>(TanksAndHealersShakeFirst)
            .TeleportAt(32f, to: new Vector2(0f, 4f))
            .ShouldKill(ActionId.EarthShakerCone, RegenHealer)
            .ShouldKillNoOneElse();

    [Test]
    public void StayingOnTheEarthShakerSpotKills()
        => Negative<UcobP3TenstrikeScenario>(RegenHealer)
            .Overrides<UcobP3TenstrikeStateOverrides>(TanksAndHealersShakeFirst)
            .FreezeAt(30.3f)
            .ShouldKill(ActionId.EarthShaker, RegenHealer)
            .ShouldKillNoOneElse();

    [Test]
    public void TheBotsClearEveryHatchAndShakerPick([Values(RegenHealer, MeleeDpsA, CasterDps)] PartyRole hatch, [Values(MainTank, PhysRangedDps)] PartyRole firstShaker)
        => Negative<UcobP3TenstrikeScenario>(hatch)
            .Overrides<UcobP3TenstrikeStateOverrides>(o =>
            {
                o.HatchInclude = hatch;
                o.FirstEarthShakerInclude = firstShaker;
            })
            .ShouldKillNoOneElse();
}
