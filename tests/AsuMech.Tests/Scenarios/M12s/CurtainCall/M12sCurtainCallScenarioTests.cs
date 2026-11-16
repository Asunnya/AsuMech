using System.Numerics;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.M12s.M12sConstants;
using static AsuMech.Tests.NegativeRun;
using Scenario = AsuMech.Scenarios.M12s.CurtainCall.M12sCurtainCallScenario;
using Overrides = AsuMech.Scenarios.M12s.CurtainCall.M12sCurtainCallStateOverrides;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M12sCurtainCallScenarioTests
{
    private static void DragonEast(Overrides o, bool rotting)
    {
        o.DragonEast = true;
        o.NorthWestSafe = true;
        o.PlayerRotting = rotting;
    }

    [Test]
    public void RottingFleshLeftOutOfTheCleaveKills()
        => Negative<Scenario>(MainTank)
            .Overrides<Overrides>(o => DragonEast(o, rotting: true))
            .TeleportAt(Scenario.SpotlightAt[^1] + 0.1f, to: new Vector2(19f, -14f))
            .ShouldKill(ActionId.CellDeath, MainTank);

    [Test]
    public void TwoBurstingGrotesqueriesOnTopOfEachOtherKillBoth()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<Overrides>(o => DragonEast(o, rotting: false))
            .TeleportAt(Scenario.SpotlightAt[^1] + 0.1f, to: new Vector2(17f, -7f))
            .ShouldKill(ActionId.DramaticLysis, MeleeDpsA, MeleeDpsB);

    [Test]
    public void AChainNeverPulledApartKillsTheirPair()
        => Negative<Scenario>(OffTank)
            .Overrides<Overrides>(o => DragonEast(o, rotting: true))
            .TeleportAt(Scenario.ChainsAt + 0.1f, to: new Vector2(0f, 0f))
            .ShouldKillSomeone(ActionId.ChainHeld);

    [Test]
    public void StayingInTheMiddleForTheBloodVesselsKills()
        => Negative<Scenario>(RegenHealer)
            .Overrides<Overrides>(o => DragonEast(o, rotting: true))
            .TeleportAt(Scenario.BurstCastAt + 0.5f, to: new Vector2(0f, 0f))
            .ShouldKill(ActionId.Burst, RegenHealer);
}
