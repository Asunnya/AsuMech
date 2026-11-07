using System.Linq;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Ucob;
using AsuMech.Scenarios.Ucob.P5GoldenBahamut;
using static AsuMech.Core.Game.Party.PartyRole;

namespace AsuMech.Tests;

public class UcobP5GoldenBahamutStateTests
{
    [Test]
    public void MornAfahMarksANonTankEachTime([Range(0, 49)] int seed)
    {
        var state = new UcobP5GoldenBahamutState(new Rng(seed));
        Assert.That(state.MornAfahTargets, Has.Count.EqualTo(UcobP5GoldenBahamutScenario.MornAfahCasts.Length));
        Assert.That(state.MornAfahTargets.Any(r => r.IsTank()), Is.False);
    }

    [TestCase(MainTank, new[] { MainTank, MainTank, OffTank, OffTank })]
    [TestCase(OffTank, new[] { MainTank, OffTank, MainTank, MainTank })]
    public void AkhMornGoesSharedThenEachTanksInvulnThenShared(PartyRole firstInvuln, PartyRole[] expected)
    {
        var state = new UcobP5GoldenBahamutState(new Rng(3), new UcobP5GoldenBahamutStateOverrides { FirstInvulnTank = firstInvuln });
        Assert.That(state.AkhMornTargets, Is.EqualTo(expected));
        Assert.That(Enumerable.Range(0, 4).Select(UcobP5GoldenBahamutState.IsInvulnAkhMorn), Is.EqualTo(new[] { false, true, true, false }));
    }

    [Test]
    public void ANonTankCannotTakeTheFirstInvuln()
        => Assert.That(new UcobP5GoldenBahamutState(new Rng(1), new UcobP5GoldenBahamutStateOverrides { FirstInvulnTank = CasterDps }).FirstInvulnTank, Is.EqualTo(MainTank));

    [Test]
    public void EachExaflareSetRollsOnItsOwnCastWithAllSixLanes([Range(0, 19)] int seed)
    {
        var state = new UcobP5GoldenBahamutState(new Rng(seed));
        Assert.That(state.Exaflares.Select(e => e.BossCastAt), Is.EqualTo(UcobP5GoldenBahamutScenario.ExaflareCasts));
        foreach (var set in state.Exaflares)
        {
            Assert.That(set.LaneOrder, Is.EquivalentTo(UcobExaflarePattern.LaneOffsets));
            Assert.That(set.Hits, Has.Count.EqualTo(UcobExaflarePattern.LaneOffsets.Count * UcobExaflarePattern.HitsPerLine));
        }
    }

    [Test]
    public void EverySetEndsBeforeTheNextMornAfahResolves([Range(0, 9)] int seed)
    {
        var state = new UcobP5GoldenBahamutState(new Rng(seed));
        foreach (var set in state.Exaflares)
        {
            var next = UcobP5GoldenBahamutScenario.MornAfahCasts.Concat(UcobP5GoldenBahamutScenario.AkhMornCasts).Where(c => c > set.BossCastAt).Min();
            Assert.That(set.LastHitAt, Is.LessThan(next + UcobP5GoldenBahamutScenario.AkhMornResolve - 2f));
        }
    }

    [Test]
    public void APinnedExaflareDirectionIsKept([Range(0, 3)] int set)
    {
        var overrides = new UcobP5GoldenBahamutStateOverrides();
        overrides.ExaflareDirections[set] = Direction.SW;
        Assert.That(new UcobP5GoldenBahamutState(new Rng(11), overrides).Exaflares[set].Direction, Is.EqualTo(Direction.SW));
    }

    [Test]
    public void ASeedReplaysTheSameRolls()
    {
        var a = new UcobP5GoldenBahamutState(new Rng(42));
        var b = new UcobP5GoldenBahamutState(new Rng(42));
        Assert.That(a.MornAfahTargets, Is.EqualTo(b.MornAfahTargets));
        Assert.That(a.Exaflares.Select(e => (e.Direction, string.Join(",", e.LaneOrder))), Is.EqualTo(b.Exaflares.Select(e => (e.Direction, string.Join(",", e.LaneOrder)))));
    }

    [Test]
    public void PinningAnExaflareLeavesTheOtherRollsAlone([Range(0, 9)] int seed)
    {
        var free = new UcobP5GoldenBahamutState(new Rng(seed));
        var overrides = new UcobP5GoldenBahamutStateOverrides();
        overrides.ExaflareDirections[0] = Direction.E;
        var pinned = new UcobP5GoldenBahamutState(new Rng(seed), overrides);
        Assert.That(pinned.MornAfahTargets, Is.EqualTo(free.MornAfahTargets));
        Assert.That(pinned.Exaflares[1].Direction, Is.EqualTo(free.Exaflares[1].Direction));
        Assert.That(pinned.Exaflares[0].LaneOrder, Is.EqualTo(free.Exaflares[0].LaneOrder));
    }
}
