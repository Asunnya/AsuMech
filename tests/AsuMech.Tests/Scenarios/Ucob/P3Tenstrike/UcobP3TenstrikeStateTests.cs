using System;
using System.Linq;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Ucob.P3Tenstrike;
using static AsuMech.Core.Game.Party.PartyRole;

namespace AsuMech.Tests;

public class UcobP3TenstrikeStateTests
{
    private static readonly PartyRole[] Everyone = Enum.GetValues<PartyRole>();

    [Test]
    public void ThreeNonTanksTakeTheHatches([Range(0, 49)] int seed)
    {
        var state = new UcobP3TenstrikeState(new Rng(seed));
        Assert.That(state.HatchTargets, Has.Count.EqualTo(UcobP3TenstrikeState.HatchCount));
        Assert.That(state.HatchTargets, Is.Unique);
        Assert.That(state.HatchTargets, Is.SubsetOf(UcobP3TenstrikeState.HatchPool));
    }

    [Test]
    public void MeteorsLandOnDpsThenHealersThenTanks([Range(0, 49)] int seed)
    {
        var state = new UcobP3TenstrikeState(new Rng(seed));
        Assert.That(state.MeteorOrder, Is.EquivalentTo(Everyone));
        Assert.That(state.MeteorOrder.Take(4), Is.EquivalentTo(new[] { MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps }));
        Assert.That(state.MeteorOrder.Skip(4).Take(2), Is.EquivalentTo(new[] { RegenHealer, ShieldHealer }));
        Assert.That(state.MeteorOrder.Skip(6), Is.EquivalentTo(new[] { MainTank, OffTank }));
    }

    [Test]
    public void TheEarthShakerSetsSplitThePartyInHalves([Range(0, 49)] int seed)
    {
        var state = new UcobP3TenstrikeState(new Rng(seed));
        Assert.That(state.FirstEarthShakers, Has.Count.EqualTo(UcobP3TenstrikeState.EarthShakersPerSet));
        Assert.That(state.FirstEarthShakers.Concat(state.SecondEarthShakers), Is.EquivalentTo(Everyone));
    }

    [Test]
    public void APickedHatchRoleIsMarked([ValueSource(typeof(UcobP3TenstrikeState), nameof(UcobP3TenstrikeState.HatchPool))] PartyRole role, [Range(0, 4)] int seed)
    {
        var state = new UcobP3TenstrikeState(new Rng(seed), new UcobP3TenstrikeStateOverrides { HatchInclude = role });
        Assert.That(state.HatchTargets, Does.Contain(role));
        Assert.That(state.HatchTargets, Has.Count.EqualTo(UcobP3TenstrikeState.HatchCount));
    }

    [Test]
    public void APickedEarthShakerSetIsKept([ValueSource(nameof(Everyone))] PartyRole role, [Values] bool first, [Range(0, 4)] int seed)
    {
        var overrides = first
            ? new UcobP3TenstrikeStateOverrides { FirstEarthShakerInclude = role }
            : new UcobP3TenstrikeStateOverrides { SecondEarthShakerInclude = role };
        var state = new UcobP3TenstrikeState(new Rng(seed), overrides);
        Assert.That(first ? state.FirstEarthShakers : state.SecondEarthShakers, Does.Contain(role));
        Assert.That(state.FirstEarthShakers, Has.Count.EqualTo(UcobP3TenstrikeState.EarthShakersPerSet));
    }
}
