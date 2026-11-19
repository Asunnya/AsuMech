using System.Linq;
using System.Numerics;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Fru.P2QuadrupleSlap;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Fru.FruConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

//   10.35 first slap on the enmity holder (MT); its vuln lands with the hit.
//   16.98 second slap on whoever holds enmity then.
public class FruP2QuadrupleSlapScenarioTests
{
    private const ushort Rampart = 1191;
    private const ushort Damnation = 3832;
    private const ushort ThrillOfBattle = 87;
    private const ushort Holmgang = 409;

    private static readonly Vector2 MainTankSpot = new(0, -4);
    private static readonly Vector2 OffTankSpot = new(2.5f, -3.5f);

    [Test]
    public void TakingTheFirstSlapWithoutMitigationKills()
        => Negative<FruP2QuadrupleSlapScenario>(MainTank)
            .WithMitigationChecks()
            .TeleportAt(5f, to: MainTankSpot)
            .ShouldKill(ActionId.QuadrupleSlap, MainTank);

    [Test]
    public void AnInvulnCarriesTheMainTankThroughBothSlaps()
        => Negative<FruP2QuadrupleSlapScenario>(MainTank)
            .WithMitigationChecks()
            .TeleportAt(5f, to: MainTankSpot)
            .At(13f, p => p.Member(MainTank)!.AddStatus(Holmgang, 10f))
            .ShouldKillNoOneElse();

    [Test]
    public void MitigationLivesThroughTheFirstSlapButNotTheSecondOnItsVuln()
        => Negative<FruP2QuadrupleSlapScenario>(MainTank)
            .WithMitigationChecks()
            .TeleportAt(5f, to: MainTankSpot)
            .At(13f, p =>
            {
                p.Member(MainTank)!.AddStatus(Rampart, 20f);
                p.Member(MainTank)!.AddStatus(Damnation, 15f);
                p.Member(MainTank)!.AddStatus(ThrillOfBattle, 10f);
            })
            .At(16.5f, p => Assert.That(p.Member(MainTank)!.HasStatus(StatusId.QuadrupleSlapVulnerability)))
            .ShouldKill(ActionId.QuadrupleSlapSecond, MainTank);

    [Test]
    public void AProvokeBetweenTheSlapsMovesTheSecondToTheOffTank()
        => Negative<FruP2QuadrupleSlapScenario>(OffTank)
            .WithMitigationChecks()
            .TeleportAt(5f, to: OffTankSpot)
            .At(16f, p => Usurper(p).Provoke(p.Member(OffTank)!))
            .ShouldKill(ActionId.QuadrupleSlapSecond, OffTank);

    [Test]
    public void TheOffTankTakesTheSecondSlapOnMitigationAfterProvoking()
        => Negative<FruP2QuadrupleSlapScenario>(OffTank)
            .WithMitigationChecks()
            .TeleportAt(5f, to: OffTankSpot)
            .At(16f, p =>
            {
                Usurper(p).Provoke(p.Member(OffTank)!);
                p.Member(OffTank)!.AddStatus(Rampart, 20f);
                p.Member(OffTank)!.AddStatus(Damnation, 15f);
                p.Member(OffTank)!.AddStatus(ThrillOfBattle, 10f);
            })
            .ShouldKillNoOneElse();

    private static SimEnemy Usurper(ScenarioProbe p)
        => p.World.Children.OfType<SimEnemy>().Single(e => e.BNpcBaseId == BNpcBaseId.UsurperP2);
}
