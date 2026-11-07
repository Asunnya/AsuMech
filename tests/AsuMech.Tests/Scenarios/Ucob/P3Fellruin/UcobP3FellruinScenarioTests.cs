using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios.Ucob.P3Fellruin;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
// Bahamut Prime lands west-northwest (-8.66, -5), Nael east-northeast (8.66, -5).
public class UcobP3FellruinScenarioTests
{
    private static void BahamutWestNael(UcobP3FellruinStateOverrides o)
    {
        o.BahamutSlot = 2;
        o.NaelClockwiseOfBahamut = true;
    }

    [Test]
    public void StayingAwayFromNaelForLunarDynamoKills()
        => Negative<UcobP3FellruinScenario>(CasterDps)
            .Overrides<UcobP3FellruinStateOverrides>(o =>
            {
                BahamutWestNael(o);
                o.Quote = 0;
            })
            .TeleportAt(17f, to: new Vector2(10f, 5f))
            .ShouldKill(ActionId.LunarDynamo, CasterDps);

    [Test]
    public void StandingNextToATankAsTempestWingGoesOffKills()
        => Negative<UcobP3FellruinScenario>(CasterDps)
            .Overrides<UcobP3FellruinStateOverrides>(BahamutWestNael)
            .TeleportAt(22.9f, to: new Vector2(2.84f, -8.4f))
            .ShouldKill(ActionId.TempestWingSplash, CasterDps);

    [Test]
    public void LeavingTheNeurolinkForAethericProfusionKills()
        => Negative<UcobP3FellruinScenario>(CasterDps)
            .Overrides<UcobP3FellruinStateOverrides>(BahamutWestNael)
            .TeleportAt(23.6f, to: new Vector2(0f, 3f))
            .ShouldKill(ActionId.AethericProfusion, CasterDps);

    [Test]
    public void SharingAMeteorStreamKillsBoth()
        => Negative<UcobP3FellruinScenario>(CasterDps)
            .Overrides<UcobP3FellruinStateOverrides>(o =>
            {
                BahamutWestNael(o);
                o.MeteorTargets = [CasterDps, PhysRangedDps, MainTank, OffTank];
            })
            .TeleportAt(26f, to: new Vector2(0.84f, -8.4f))
            .ShouldKill(ActionId.MeteorStream, CasterDps, MainTank);

    [Test]
    public void TheBotsClearEveryQuoteAndLayout([Values(0, 1)] int quote, [Values(0, 1, 2)] int slot, [Values] bool naelClockwise)
        => Negative<UcobP3FellruinScenario>(PhysRangedDps)
            .Overrides<UcobP3FellruinStateOverrides>(o =>
            {
                o.Quote = quote;
                o.BahamutSlot = slot;
                o.NaelClockwiseOfBahamut = naelClockwise;
            })
            .ShouldKillNoOneElse();

    [Test]
    public void TheBotsClearWithTheDiveOnATank([Values(0, 1)] int quote, [Values(MainTank, OffTank)] PartyRole diver)
        => Negative<UcobP3FellruinScenario>(PhysRangedDps)
            .Overrides<UcobP3FellruinStateOverrides>(o =>
            {
                o.Quote = quote;
                o.RavenDiveTarget = diver;
            })
            .ShouldKillNoOneElse();
}
