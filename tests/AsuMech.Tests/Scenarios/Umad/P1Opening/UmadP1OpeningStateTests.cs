using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Umad.P1Opening;

namespace AsuMech.Tests;

public class UmadP1OpeningStateTests
{
    private static IEnumerable<UmadP1OpeningState> Seeds(UmadP1OpeningStateOverrides? overrides = null)
        => Enumerable.Range(0, 300).Select(seed => new UmadP1OpeningState(new Rng(seed), overrides));

    [Test]
    public void WaveCannonPicksTwoSupportsAndTwoDps()
    {
        foreach (var state in Seeds())
        {
            Assert.That(state.LaserTargets.Distinct().Count(), Is.EqualTo(4));
            Assert.That(state.LaserTargets.Count(r => r.IsDps()), Is.EqualTo(2));
        }
    }

    [Test]
    public void EveryDoubleTroubleTrapSitsOnOneSupportAndOneDps()
    {
        foreach (var state in Seeds())
            foreach (var (support, dps) in state.Confetti)
            {
                Assert.That(support.IsDps(), Is.False);
                Assert.That(dps.IsDps(), Is.True);
            }
    }

    [Test]
    public void TheFireStacksGoOnOneHealerAndOneDps()
    {
        foreach (var state in Seeds())
        {
            Assert.That(state.FireStackSupport, Is.AnyOf(PartyRole.RegenHealer, PartyRole.ShieldHealer));
            Assert.That(state.FireStackDps.IsDps(), Is.True);
            Assert.That(state.FireShowsStackIcon, Is.EqualTo(state.FireIsStack != state.FireIsLie));
        }
    }

    [Test]
    public void AForcedRoleGetsItsLaserStackAndConfettiWhicheverGroupItIsIn()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
            foreach (var state in Seeds(new UmadP1OpeningStateOverrides { LaserOn = role, FireStackOn = role, FirstConfettiOn = role, SecondConfettiOn = role }))
            {
                Assert.That(state.IsLasered(role));
                Assert.That(role == state.FireStackSupport || role == state.FireStackDps, Is.EqualTo(!role.IsTank()));
                Assert.That(role == state.Confetti[0].Support || role == state.Confetti[0].Dps);
                Assert.That(role == state.Confetti[1].Support || role == state.Confetti[1].Dps);
            }
    }

    [Test]
    public void PinnedChoicesHold()
    {
        foreach (var pinned in new[] { true, false })
            foreach (var state in Seeds(new UmadP1OpeningStateOverrides
                     {
                         TetheredAreDps = pinned, FireIsStack = pinned, FireIsLie = pinned, FirstIceIsLie = pinned, SecondIceIsLie = pinned,
                         ThunderIsLie = pinned, ThirdIceIsLie = pinned, FirstGravitasOnDps = pinned, SecondGravitasOnDps = !pinned,
                         FirstWillEast = pinned, SecondWillEast = !pinned,
                     }))
            {
                Assert.That(state.TetheredAreDps, Is.EqualTo(pinned));
                Assert.That(state.FireIsStack, Is.EqualTo(pinned));
                Assert.That(state.FireIsLie, Is.EqualTo(pinned));
                Assert.That(new[] { state.FirstIce.IsLie, state.SecondIce.IsLie, state.ThirdIce.IsLie, state.ThunderIsLie }, Has.All.EqualTo(pinned));
                Assert.That(state.GravitasOnDps, Is.EqualTo(new[] { pinned, !pinned }));
                Assert.That(state.WillEast, Is.EqualTo(new[] { pinned, !pinned }));
            }
    }

    [Test]
    public void PinningOneChoiceLeavesTheOtherRollsAsTheSeedHadThem()
    {
        for (var seed = 0; seed < 100; seed++)
        {
            var free = new UmadP1OpeningState(new Rng(seed));
            var pinned = new UmadP1OpeningState(new Rng(seed), new UmadP1OpeningStateOverrides { FireIsLie = !free.FireIsLie });
            Assert.That(pinned.LaserTargets, Is.EqualTo(free.LaserTargets));
            Assert.That(pinned.FirstIce.RealNwSe, Is.EqualTo(free.FirstIce.RealNwSe));
            Assert.That(pinned.WillEast, Is.EqualTo(free.WillEast));
        }
    }

    [Test]
    public void ALyingIceSetShowsTheOtherPairOfCones()
    {
        Assert.That(new IceSet(RealNwSe: true, IsLie: true).ShownNwSe, Is.False);
        Assert.That(new IceSet(RealNwSe: true, IsLie: false).ShownNwSe, Is.True);
    }
}
