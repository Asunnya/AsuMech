using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Ucob.P3Quickmarch;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using static AsuMech.Tests.NegativeRun;
using State = AsuMech.Scenarios.Ucob.P3Quickmarch.UcobP3QuickmarchState;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the dragons dive from the north, Bahamut sits at the
// centre, and the player follows the strat until taken over.
public class UcobP3QuickmarchScenarioTests
{
    private static NegativeRun<UcobP3QuickmarchScenario> FromTheNorth(PartyRole role, System.Action<UcobP3QuickmarchStateOverrides>? set = null)
        => Negative<UcobP3QuickmarchScenario>(role).Overrides<UcobP3QuickmarchStateOverrides>(o =>
        {
            o.DiveFrom = Direction.N;
            set?.Invoke(o);
        });

    [Test]
    public void StandingInTheDivesKills()
        => FromTheNorth(MeleeDpsA)
            .TeleportAt(14f, to: new Vector2(0f, 0f))
            .ShouldKill(ActionId.MegaflareDive, MeleeDpsA);

    [Test]
    public void StayingOnYourSpotAfterTheDivesTouchesYourTwister()
        => FromTheNorth(CasterDps, o => o.TwisterTargets = [CasterDps, MeleeDpsA, RegenHealer, MainTank])
            .FreezeAt(16f)
            .ShouldKill(ActionId.TwisterBlast, CasterDps);

    [Test]
    public void StayingInYourMegaflarePuddleKills()
        => FromTheNorth(PhysRangedDps, o => o.PuddleTargets = [MainTank, RegenHealer, PhysRangedDps, MeleeDpsB])
            .FreezeAt(19f)
            .ShouldKill(ActionId.MegaflarePuddle, PhysRangedDps);

    [Test]
    public void StandingOnAnotherPlayersSpreadKills()
        => FromTheNorth(PhysRangedDps, o => o.SpreadTargets = [RegenHealer, MeleeDpsA, MeleeDpsB, CasterDps])
            .TeleportAt(19.2f, to: new Vector2(13.79f, 2.43f))
            .ShouldKill(ActionId.MegaflareSpread, PhysRangedDps);

    [Test]
    public void MissingTheMegaflareStackKillsYouThenTheShortStack()
        => FromTheNorth(MeleeDpsA, o => o.EarthShakerDps = CasterDps)
            .TeleportAt(22.5f, to: new Vector2(-13.79f, 2.43f))
            .ShouldKill(ActionId.MegaflareStack, MeleeDpsA)
            .ShouldKill(ActionId.MegaflareStack, MeleeDpsB, PhysRangedDps);

    [Test]
    public void PointingYourEarthShakerAtAHealerKillsYouBothAndTheTankBetween()
        => FromTheNorth(CasterDps, o => o.EarthShakerDps = CasterDps)
            .TeleportAt(25f, to: new Vector2(7f, -7f))
            .ShouldKill(ActionId.EarthShakerCone, CasterDps, ShieldHealer, OffTank);

    [Test]
    public void StandingNextToATetheredTankKills()
        => FromTheNorth(PhysRangedDps, o => o.EarthShakerDps = CasterDps)
            .TeleportAt(28.6f, to: new Vector2(-3.5f, -1.5f))
            .ShouldKill(ActionId.TempestWingBlast, PhysRangedDps);

    [Test]
    public void StandingInFrontOfBahamutForFlareBreathKills()
        => FromTheNorth(RegenHealer)
            .TeleportAt(31.5f, to: new Vector2(0f, -8f))
            .ShouldKill(ActionId.FlareBreath, RegenHealer);

    [Test]
    public void TheBotsClearFromEveryDirection([ValueSource(typeof(Direction), nameof(Direction.All))] Direction from)
        => Negative<UcobP3QuickmarchScenario>(OffTank)
            .Overrides<UcobP3QuickmarchStateOverrides>(o => o.DiveFrom = from)
            .ShouldKillNoOneElse();

    [Test]
    public void TheBotsClearWhicheverDpsTakesTheEarthShaker([ValueSource(typeof(State), nameof(State.Dps))] PartyRole dps)
        => Negative<UcobP3QuickmarchScenario>(MainTank)
            .Overrides<UcobP3QuickmarchStateOverrides>(o => o.EarthShakerDps = dps)
            .ShouldKillNoOneElse();
}
