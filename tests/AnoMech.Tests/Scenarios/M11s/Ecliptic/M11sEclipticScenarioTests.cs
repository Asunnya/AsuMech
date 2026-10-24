using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.M11s.Ecliptic;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.M11s.M11sConstants;
using static AnoMech.Tests.NegativeRun;
using Scenario = AnoMech.Scenarios.M11s.Ecliptic.M11sEclipticScenario;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M11sEclipticScenarioTests
{
    [Test]
    public void TethersStretchAcrossThenTurnClockwise()
    {
        Assert.That(M11sEclipticState.CornerForShade(1), Is.EqualTo(3));
        Assert.That(M11sEclipticState.CornerForShade(2), Is.EqualTo(0));
        Assert.That(M11sEclipticState.CornerForShade(0), Is.EqualTo(2));
        Assert.That(M11sEclipticState.CornerForShade(3), Is.EqualTo(1));
        for (var seed = 0; seed < 200; seed++)
        {
            var state = new M11sEclipticState(new Rng(seed));
            Assert.That(state.SingleTowers.Distinct().Count(), Is.EqualTo(2));
            Assert.That(state.ShadeForTower, Is.EquivalentTo(new[] { 0, 1, 2, 3 }));
            Assert.That(state.MammothSpots().Select(m => (int)MathF.Round(MathF.Atan2(m.X, m.Z) * 2 / MathF.PI)), Is.Unique);
        }
    }

    [Test]
    public void AMarkedPlayerStandingStillBurnsInTheirPuddle()
        => Negative<Scenario>(PhysRangedDps)
            .Overrides<M11sEclipticStateOverrides>(o => o.PlayerMarked = true)
            .FreezeAt(Scenario.AtomicHitAt[0] - 0.2f)
            .ShouldKill(ActionId.AtomicImpact, PhysRangedDps);

    [Test]
    public void StandingByAMammothMeteorKills()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M11sEclipticStateOverrides>(o => o.MammothNortheast = true)
            .TeleportAt(Scenario.MammothCastAt + 2f, to: new Vector2(13f, -13f))
            .ShouldKill(ActionId.MammothMeteor, MeleeDpsA);

    [Test]
    public void AnUnstretchedTetherKillsItsHolder()
        => Negative<Scenario>(MainTank)
            .Seed(1)
            .TeleportAt(Scenario.WrathAt - 1f, to: new Vector2(0f, 0f))
            .ShouldKill(ActionId.EclipticMeteowrath, MainTank);

    [Test]
    public void JoiningTheMainTankInTheFirstKickTowerKills()
        => Negative<Scenario>(MeleeDpsA)
            .TeleportAt(Scenario.KickCastAt[0] + 1f, to: new Vector2(1f, -1f))
            .ShouldKill(ActionId.HeartbreakKickTower, MeleeDpsA);

    [Test]
    public void TheMainTankTakesTheFirstKickTowerOnlyWithAnInvuln()
        => Negative<Scenario>(MainTank)
            .WithMitigationChecks()
            .TeleportAt(Scenario.KickCastAt[0] + 1f, to: new Vector2(0f, -2f))
            .ShouldKill(ActionId.HeartbreakKickTower, MainTank);

    [Test]
    public void LeavingTheLastKickTowerShortKillsThePartyInIt()
        => Negative<Scenario>(MeleeDpsA)
            .TeleportAt(Scenario.KickCastAt[2] + 1f, to: new Vector2(15f, 15f))
            .ShouldKill(ActionId.HeartbreakKickTower, RegenHealer, ShieldHealer, MeleeDpsB, PhysRangedDps, CasterDps);

    private static IEnumerable<int> Seeds() => Enumerable.Range(0, 40);

    // Nobody takes an unsoaked tower or dies before the player walks off the arena after the kicks.
    [TestCaseSource(nameof(Seeds))]
    public void TheStratSoaksEveryTowerAndClearsEveryLine(int seed)
        => Negative<Scenario>(MeleeDpsA)
            .Seed(seed)
            .At(Scenario.CrownCastAt[1] + 1f, p =>
            {
                foreach (var role in Enum.GetValues<PartyRole>())
                {
                    Assert.That(p.Member(role)!.HasStatus(StatusId.DamageDown), Is.False, $"{role} took an unsoaked tower");
                    Assert.That(p.Member(role)!.IsAlive(), Is.True, $"{role} died");
                }
            })
            .TeleportAt(Scenario.HeartbreakerCastAt, to: new Vector2(25f, 0f))
            .ShouldKill(TheEnvironment, MeleeDpsA);
}
