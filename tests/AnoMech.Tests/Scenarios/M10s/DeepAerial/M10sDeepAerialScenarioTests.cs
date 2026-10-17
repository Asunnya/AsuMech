using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.M10s.DeepAerial;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.M10s.M10sConstants;
using static AnoMech.Tests.NegativeRun;
using Scenario = AnoMech.Scenarios.M10s.DeepAerial.M10sDeepAerialScenario;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M10sDeepAerialScenarioTests
{
    // The brothers start north: Red Hot at (14,-14), Deep Blue at (-14,-14).
    private static void Pinned(M10sDeepAerialStateOverrides o)
    {
        o.BossesNorth = true;
        o.Tethers =
        [
            (MainTank, MeleeDpsA), (OffTank, MeleeDpsB), (PhysRangedDps, CasterDps),
            (MeleeDpsA, MainTank), (MeleeDpsB, OffTank), (CasterDps, PhysRangedDps),
        ];
    }

    [Test]
    public void AShortTetherKillsItsTarget()
        => Negative<Scenario>(MainTank)
            .Overrides<M10sDeepAerialStateOverrides>(Pinned)
            .TeleportAt(Scenario.TetherAt[0] + 1f, to: new Vector2(14f, 0f))
            .ShouldKill(ActionId.XtremeWaveFire, MainTank);

    [Test]
    public void AWaterWaveThroughThePrisonDrownsThePrisoners()
        => Negative<Scenario>(PhysRangedDps)
            .Overrides<M10sDeepAerialStateOverrides>(Pinned)
            .MoveBotAt(Scenario.TetherAt[0] + 1f, MeleeDpsA, new Vector2(4f, 16f))
            .MoveBotAt(Scenario.TetherAt[0] + Scenario.TetherToWave + 0.3f, MeleeDpsA, new Vector2(8f, 0f))
            .ShouldKill(ActionId.ScathingSteam, RegenHealer, ShieldHealer);

    [Test]
    public void AFireWaveMissingThePrisonDrownsThePrisoners()
        => Negative<Scenario>(PhysRangedDps)
            .Overrides<M10sDeepAerialStateOverrides>(Pinned)
            .MoveBotAt(Scenario.TetherAt[0] + 1f, MainTank, new Vector2(12f, 17f))
            .MoveBotAt(Scenario.TetherAt[0] + Scenario.TetherToWave + 0.3f, MainTank, new Vector2(8f, 0f))
            .ShouldKill(ActionId.ScathingSteam, RegenHealer, ShieldHealer);

    [Test]
    public void AnEmptyTowerWipes()
        => Negative<Scenario>(RegenHealer)
            .Overrides<M10sDeepAerialStateOverrides>(Pinned)
            .TeleportAt(5f, to: new Vector2(0f, 12f))
            .MoveBotAt(5f, ShieldHealer, new Vector2(0f, -12f))
            .ShouldKillSomeone(ActionId.UnmitigatedExplosion);

    [Test]
    public void TheHealersAreLockedInTheGraves()
        => Negative<Scenario>(MainTank)
            .Overrides<M10sDeepAerialStateOverrides>(Pinned)
            .At(Scenario.TowerHitAt + 2f, p =>
            {
                Assert.That(new[] { RegenHealer, ShieldHealer }.Select(r => p.Member(r)!.HasStatus(StatusId.WateryGrave)), Is.All.True);
                Assert.That(Vector3.Distance(p.Member(RegenHealer)!.Position, Scenario.GraveSpots[0]), Is.LessThan(0.6f));
            })
            .TeleportAt(Scenario.TetherAt[0] + 1f, to: new Vector2(14f, 0f))
            .ShouldKill(ActionId.XtremeWaveFire, MainTank);
}
