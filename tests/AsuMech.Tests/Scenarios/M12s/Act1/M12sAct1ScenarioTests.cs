using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios.M12s.Act1;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.M12s.M12sConstants;
using static AsuMech.Tests.NegativeRun;
using Scenario = AsuMech.Scenarios.M12s.Act1.M12sAct1Scenario;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M12sAct1ScenarioTests
{
    private const float FaceNorth = MathF.PI;
    private const float FaceEast = MathF.PI / 2f;
    private const float FaceSouth = 0f;

    // East safe; the party ends in the north-east corner and the tanks on the west wall.
    private static Action<M12sAct1StateOverrides> Pinned(Act1Debuff debuff, PartyRole stackHealer = ShieldHealer) => o =>
    {
        o.PlayerDebuff = debuff;
        o.PlayerArrow = GrotesquerieArrow.Front;
        o.EastSafe = true;
        o.VesselLayout = 0;
        o.StackHealer = stackHealer;
    };

    [Test]
    public void OverlappingBurstingSpreadsKillBoth()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides(Pinned(Act1Debuff.Spread))
            .TeleportAt(Scenario.GrotesquerieGoesOffAt - 0.5f, to: new Vector2(10f, -6.4f), facing: FaceNorth)
            .ShouldKill(ActionId.DramaticLysis, MeleeDpsA, MeleeDpsB);

    [Test]
    public void AnArrowCleaveThroughTheStackKillsIt()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides(Pinned(Act1Debuff.Spread))
            .TeleportAt(Scenario.GrotesquerieGoesOffAt - 0.5f, to: new Vector2(4.5f, -12.4f), facing: FaceEast)
            .ShouldKill(ActionId.HemorrhagicProjection, MainTank, OffTank, RegenHealer, ShieldHealer);

    [Test]
    public void TakingSharedGrotesquerieAloneKills()
        => Negative<Scenario>(RegenHealer)
            .Overrides(Pinned(Act1Debuff.Shared))
            .TeleportAt(Scenario.GrotesquerieGoesOffAt - 0.5f, to: new Vector2(0f, 5f), facing: FaceSouth)
            .ShouldKill(ActionId.FourthWallFusion, RegenHealer);

    [Test]
    public void StayingNearABloodVesselKills()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides(Pinned(Act1Debuff.Spread))
            .FreezeAt(25f)
            .ShouldKill(ActionId.Burst, MeleeDpsA);

    [Test]
    public void AHealerUnderTheTankBustersDies()
        => Negative<Scenario>(RegenHealer)
            .Overrides(Pinned(Act1Debuff.Stack))
            .TeleportAt(Scenario.BustersAt - 0.5f, to: new Vector2(-18f, 0.5f))
            .ShouldKill(ActionId.VisceralBurst, RegenHealer);
}
