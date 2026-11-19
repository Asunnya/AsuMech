using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Fru.P1TurnOfTheHeavens;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Fru.FruConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Pinned: the image's Cyclonic Break lightning, Turn of the Heavens fire with the halos of flame
// west, so the small halos and the safe half are east; Bound of Faith on MT and M1.
//
//   18.89 / 20.6 Burnt Strike from (-20,0) east, 10y then 20y wide.
//   24.96 Burnt Strike from (0,-20) south, 10y wide; 26.96 Blastburn pushes 15y away from it, the
//         party from (1.5,±7) to (16.5,±7).
//   29.13 Brightfire: halos of flame (10y) on N NW W SW, of levin (5y) on S SE E NE, all 15y out.
//   32.94 Sinsmoke on MT with the supports at (5.5,-4.5), on M1 with the DPS at (5.5,4.5).
public class FruP1TurnOfTheHeavensScenarioTests
{
    private static NegativeRun<FruP1TurnOfTheHeavensScenario> Heavens(PartyRole player, Action<FruP1TurnOfTheHeavensStateOverrides>? tweak = null)
        => Negative<FruP1TurnOfTheHeavensScenario>(player)
            .Overrides<FruP1TurnOfTheHeavensStateOverrides>(o =>
            {
                o.CyclonicFire = false;
                o.HeavensFire = true;
                o.FlameWest = true;
                foreach (var role in PerRole.All)
                    o.BoundOfFaith[role] = role is MainTank or MeleeDpsA ? BoundOfFaithDuty.Tethered : BoundOfFaithDuty.Untethered;
                tweak?.Invoke(o);
            });

    [Test]
    public void StandingOnTheWestStrikeDies()
        => Heavens(OffTank)
            .TeleportAt(18.3f, to: new(7, 0))
            .ShouldKill(ActionId.BurntStrikeWest, OffTank);

    [Test]
    public void StandingInBurnoutDies()
        => Heavens(OffTank)
            .TeleportAt(20f, to: new(7, 8))
            .ShouldKill(ActionId.BurnoutWest, OffTank);

    [Test]
    public void StandingOnTheNorthStrikeDies()
        => Heavens(OffTank)
            .TeleportAt(24.4f, to: new(2, -13))
            .ShouldKill(ActionId.BurntStrikeNorth, OffTank);

    [Test]
    public void BlastburnFromTheSafeHalfHitsTheWall()
        => Heavens(OffTank)
            .TeleportAt(26.3f, to: new(8, 7))
            .ShouldKill(TheEnvironment, OffTank);

    [Test]
    public void StandingInAMatchingHalosCircleDies()
        => Heavens(OffTank)
            .TeleportAt(28.5f, to: new(-8, 0))
            .ShouldKill(ActionId.BrightfireLarge, OffTank);

    [Test]
    public void StandingOnAnOtherHaloDies()
        => Heavens(OffTank)
            .TeleportAt(28.5f, to: new(15, 1))
            .ShouldKill(ActionId.BrightfireSmall, OffTank);

    [Test]
    public void SinsmokeAloneDies()
        => Heavens(MainTank)
            .TeleportAt(32.6f, to: new(5.5f, 13))
            .ShouldKill(ActionId.BoundOfFaithSinsmoke, MainTank);
}

public class FruP1TurnOfTheHeavensStateTests
{
    [Test]
    public void ThePlayersBoundOfFaithChoiceHolds([Values] PartyRole player, [Values] BoundOfFaithDuty duty, [Range(0, 4)] int seed)
    {
        var overrides = new FruP1TurnOfTheHeavensStateOverrides();
        overrides.BoundOfFaith.Mine = duty;
        var state = new FruP1TurnOfTheHeavensState(new Rng(seed), player, overrides);
        Assert.That(state.Tethered.Distinct().Count(), Is.EqualTo(2));
        Assert.That(state.IsTethered(player), Is.EqualTo(duty == BoundOfFaithDuty.Tethered));
    }

    [Test]
    public void TheSmallHalosAreTheOtherElement([Values] bool heavensFire, [Values] bool flameWest)
    {
        var state = new FruP1TurnOfTheHeavensState(new Rng(0), MainTank,
            new FruP1TurnOfTheHeavensStateOverrides { HeavensFire = heavensFire, FlameWest = flameWest });
        Assert.That(state.SmallHalosWest, Is.EqualTo(heavensFire ? !flameWest : flameWest));
    }
}
