using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Fru.P1FallOfFaith;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Fru.FruConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Pinned: Fall of Faith on M1 (lightning), M2 (fire), R (lightning), C (fire); Burnt Strike fire
// with its towers west, two soakers each.
//
//   16.0  Sinsmite on M1 at (-5.5,0); 16.3 Bow Shock from M1 at RH (-5.5,-2.5), SH (-5.5,2), R (-7.5,0).
//   19.03 Sinblaze from M2 at (5.5,0) toward C (7,0), with MT (7.6,-0.8) and OT (7.6,0.8).
//   51.84 Burnt Strike down the middle; 53.84 Blastburn pushes the soakers from (-0.5,z) to (-15.5,z),
//         the tanks from (0.5,±5.5) to (15.5,±5.5).
//   55.84 towers: RH M1 north (-13.1,-9.2), SH M2 middle (-16,0), C R south (-13.1,9.2).
//   ~56.4 Burn Mark on OT and the nearest, MT.
public class FruP1FallOfFaithScenarioTests
{
    private static readonly Dictionary<PartyRole, FallOfFaithTether> Tethers = new()
    {
        [MeleeDpsA] = FallOfFaithTether.First,
        [MeleeDpsB] = FallOfFaithTether.Second,
        [PhysRangedDps] = FallOfFaithTether.Third,
        [CasterDps] = FallOfFaithTether.Fourth,
    };

    private static NegativeRun<FruP1FallOfFaithScenario> Faith(PartyRole player)
        => Negative<FruP1FallOfFaithScenario>(player)
            .Overrides<FruP1FallOfFaithStateOverrides>(o =>
            {
                foreach (var role in PerRole.All)
                    o.Tether[role] = Tethers.GetValueOrDefault(role, FallOfFaithTether.None);
                o.FaithFire = [false, true, false, true];
                o.TowersWest = true;
                o.TowersFire = true;
                o.TowerPattern = 9;
            });

    [Test]
    public void TwoInOneBowShockDie()
        => Faith(RegenHealer)
            .TeleportAt(15.8f, to: new(-5f, 2.4f))
            .ShouldKill(ActionId.BowShock, RegenHealer, ShieldHealer);

    [Test]
    public void LeavingSinblazeShortKillsTheStack()
        => Faith(MainTank)
            .TeleportAt(18.8f, to: new(12, 8))
            .ShouldKill(ActionId.Sinblaze, MeleeDpsB, CasterDps, OffTank);

    [Test]
    public void StandingOnTheCentreLineDies()
        => Faith(RegenHealer)
            .TeleportAt(51.4f, to: new(0, 3))
            .ShouldKill(ActionId.BurntStrikeFire, RegenHealer);

    [Test]
    public void BlastburnFromFarOutHitsTheWall()
        => Faith(RegenHealer)
            .TeleportAt(53.3f, to: new(-6, -9.18f))
            .ShouldKill(TheEnvironment, RegenHealer);

    [Test]
    public void AnUnsoakedTowerWipes()
        => Faith(RegenHealer)
            .TeleportAt(55f, to: new(-6, -12))
            .ShouldKill(ActionId.UnmitigatedExplosionFire, PerRole.All);

    [Test]
    public void StandingNearestThePowderMarkDies()
        => Faith(ShieldHealer)
            .TeleportAt(55.9f, to: new(14, 7))
            .ShouldKill(ActionId.BurnMark, ShieldHealer);
}

public class FruP1FallOfFaithStateTests
{
    [Test]
    public void ThePlayersTetherChoiceHolds([Values] PartyRole player, [Values] FallOfFaithTether slot, [Range(0, 4)] int seed)
    {
        var overrides = new FruP1FallOfFaithStateOverrides();
        overrides.Tether.Mine = slot;
        overrides.Tether[player] = slot;
        var state = new FruP1FallOfFaithState(new Rng(seed), player, overrides);
        Assert.That(state.Tethered.Distinct().Count(), Is.EqualTo(4));
        Assert.That(state.FaithIndex(player), Is.EqualTo((int)slot - 1));
    }

    [Test]
    public void EveryTowerPatternTakesSix()
        => Assert.That(FruP1FallOfFaithState.TowerPatterns.Select(p => p.Sum()), Is.All.EqualTo(6));
}
