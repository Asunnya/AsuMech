using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Ucob;
using AsuMech.Scenarios.Ucob.P1Twintania;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using static AsuMech.Tests.NegativeRun;
using Scenario = AsuMech.Scenarios.Ucob.P1Twintania.UcobP1TwintaniaScenario;

namespace AsuMech.Tests;

public class UcobP1TwintaniaAiTests
{
    private const int Tuufless = 1;

    private static Vector2 At(ScenarioProbe p, PartyRole role) => new(p.Member(role)!.Position.X, p.Member(role)!.Position.Z);

    private static Vector2 ElementalMarker(WaymarkSlot slot)
    {
        var at = ElementalWaymarks.Single(w => w.Slot == slot).Offset;
        return new Vector2(at.X, at.Z);
    }

    private static PartyRole FurthestFrom(ScenarioProbe p, Vector2 from)
        => PerRole.All.OrderByDescending(r => Vector2.Distance(At(p, r), from)).First();

    [Test]
    public void TuuflessPlaysOnTheElementalMarkers()
        => Assert.That(new Scenario().AiStrats[Tuufless], Is.InstanceOf<IUcobStrat>().And.Property(nameof(IUcobStrat.Markers)).EqualTo(UcobMarkers.Elemental));

    [Test]
    public void TuuflessHatchTargetsResolveInTheNeurolinksOnMarkersOneAndTwo()
        => Negative<Scenario>(MainTank, Tuufless)
            .Overrides<UcobP1TwintaniaStateOverrides>(o =>
            {
                o.HatchTargets = [[MeleeDpsA], [MeleeDpsB], [MeleeDpsA, CasterDps], [MeleeDpsB, CasterDps], [MeleeDpsA, CasterDps]];
                o.LiquidHellTarget = RegenHealer;
            })
            .At(46.5f, p =>
            {
                Assert.That(Vector2.Distance(At(p, MeleeDpsA), ElementalMarker(WaymarkSlot.One)), Is.LessThan(1.5f));
                Assert.That(p.Member(MeleeDpsA)!.HasStatus(StatusId.Neurolink));
            })
            .At(93.5f, p =>
            {
                Assert.That(Vector2.Distance(At(p, MeleeDpsA), ElementalMarker(WaymarkSlot.One)), Is.LessThan(1.5f));
                Assert.That(Vector2.Distance(At(p, CasterDps), ElementalMarker(WaymarkSlot.Two)), Is.LessThan(2f));
                Assert.That(p.Member(MeleeDpsA)!.HasStatus(StatusId.Neurolink) && p.Member(CasterDps)!.HasStatus(StatusId.Neurolink));
            })
            .ShouldKillNoOneElse();

    [Test]
    public void TuuflessPhysRangedBaitsLiquidHell()
        => Negative<Scenario>(MainTank, Tuufless)
            .Overrides<UcobP1TwintaniaStateOverrides>(o => o.HatchTargets = [[MeleeDpsA], [MeleeDpsB], [MeleeDpsA, CasterDps], [MeleeDpsB, CasterDps], [MeleeDpsA, CasterDps]])
            .At(Scenario.LiquidHellDrops[0][0], p => Assert.That(FurthestFrom(p, UcobMarkers.Elemental.Place(Scenario.FirstWestCamp)), Is.EqualTo(PhysRangedDps)))
            .At(Scenario.LiquidHellDrops[1][0], p => Assert.That(FurthestFrom(p, UcobMarkers.Elemental.Place(Scenario.FirstWestCamp)), Is.EqualTo(PhysRangedDps)))
            .ShouldKillNoOneElse();

    [Test]
    public void TuuflessShieldHealerBaitsWhenThePhysRangedHasTheHatch()
        => Negative<Scenario>(MainTank, Tuufless)
            .Overrides<UcobP1TwintaniaStateOverrides>(o => o.HatchTargets = [[PhysRangedDps], [MeleeDpsB], [MeleeDpsA, CasterDps], [PhysRangedDps, CasterDps], [MeleeDpsA, CasterDps]])
            .At(Scenario.LiquidHellDrops[1][0], p => Assert.That(FurthestFrom(p, UcobMarkers.Elemental.Place(Scenario.FirstWestCamp)), Is.EqualTo(ShieldHealer)))
            .ShouldKillNoOneElse();

    [TestCase(RegenHealer, PhysRangedDps)]
    [TestCase(PhysRangedDps, CasterDps)]
    public void TuuflessFirstFireballIsAFiveManStack(PartyRole target, PartyRole outside)
        => Negative<Scenario>(MainTank, Tuufless)
            .Overrides<UcobP1TwintaniaStateOverrides>(o => o.FireballTargets[0] = target)
            .At(Scenario.FireballHits[0] - 0.05f, p =>
            {
                var at = At(p, target);
                var stacked = PerRole.All.Where(r => Vector2.Distance(At(p, r), at) <= UcobP1TwintaniaState.FireballRadius).ToList();
                Assert.That(stacked, Has.Count.EqualTo(5).And.Not.Contains(outside));
            })
            .ShouldKillNoOneElse();
}
