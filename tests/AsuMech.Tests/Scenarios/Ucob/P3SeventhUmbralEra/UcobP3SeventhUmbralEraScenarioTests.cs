using System.Numerics;
using AsuMech.Scenarios.Ucob.P3SeventhUmbralEra;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using static AsuMech.Tests.NegativeRun;
using Scenario = AsuMech.Scenarios.Ucob.P3SeventhUmbralEra.UcobP3SeventhUmbralEraScenario;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; Bahamut Prime sits at the centre and the main tank
// holds him north from Bahamut's targetable on.
public class UcobP3SeventhUmbralEraScenarioTests
{
    private const int Tuufless = 1;

    [Test]
    public void StandingNearTheEdgeForTheKnockbackKills()
        => Negative<Scenario>(PhysRangedDps)
            .TeleportAt(Scenario.SeventhUmbralEra - 0.5f, to: new Vector2(0f, 16f))
            .ShouldKill(TheEnvironment, PhysRangedDps);

    [Test]
    public void StandingInFrontOfBahamutForFlareBreathKills()
        => Negative<Scenario>(RegenHealer)
            .TeleportAt(Scenario.FlareBreath - 1f, to: new Vector2(0f, -8f))
            .ShouldKill(ActionId.FlareBreath, RegenHealer);

    [Test]
    public void TheBotsClear([Values(0, Tuufless)] int strat, [Values(1, 2, 3)] int seed)
        => Negative<Scenario>(OffTank, strat)
            .Seed(seed)
            .ShouldKillNoOneElse();
}
