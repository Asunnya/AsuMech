using System.Numerics;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.M12s.M12sConstants;
using static AsuMech.Tests.NegativeRun;
using Scenario = AsuMech.Scenarios.M12s.Finale.M12sFinaleScenario;

namespace AsuMech.Tests;

public class M12sFinaleScenarioTests
{
    [Test]
    public void TheSecondKickFollowsTheFirst()
        => Negative<Scenario>(MainTank)
            .FreezeAt(Scenario.FirstKickAt)
            .ShouldKill(ActionId.DoubleSobatSecond, MainTank);

    [Test]
    public void TanksSharingEsotericFinisherBothDie()
        => Negative<Scenario>(OffTank)
            .TeleportAt(Scenario.FinisherAt - 2f, to: new Vector2(-8f, -4f))
            .ShouldKill(ActionId.EsotericFinisher, MainTank, OffTank);

    [Test]
    public void StandingInFrontForDoubleSobatKills()
        => Negative<Scenario>(RegenHealer)
            .TeleportAt(Scenario.SobatCastAt, to: new Vector2(0f, -7f))
            .ShouldKill(ActionId.DoubleSobatFirst, RegenHealer);
}
