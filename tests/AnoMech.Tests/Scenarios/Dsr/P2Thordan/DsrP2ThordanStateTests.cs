using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr.P2Thordan;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Tests;

public class DsrP2ThordanStateTests
{
    private static readonly PartyRole[] AllRoles = Enum.GetValues<PartyRole>();

    private static IEnumerable<DsrP2ThordanState> Rolls(int count = 200)
        => Enumerable.Range(0, count).Select(seed => new DsrP2ThordanState(new Rng(seed)));

    [Test]
    public void KnightsDashAlongEveryLineButTheSafeOne()
    {
        foreach (var state in Rolls())
        {
            var lines = state.DashBearings.Select(b => b % 180f).ToList();
            Assert.That(lines, Is.Unique);
            Assert.That(lines, Has.None.EqualTo(state.SafeLineBearing));
        }
    }

    [Test]
    public void DefamationsAndStackGoOnDifferentNonTanks()
    {
        foreach (var state in Rolls())
        {
            var marked = state.Defamations.Concat(state.Stackers).ToList();
            Assert.That(marked, Is.Unique.And.Count.EqualTo(6));
            Assert.That(marked, Has.None.EqualTo(MainTank).And.None.EqualTo(OffTank));
            Assert.That(state.Stackers, Does.Contain(state.StackTarget));
            Assert.That(state.ShieldBashTethers, Is.Unique.And.SubsetOf(state.Stackers));
        }
    }

    [Test]
    public void PlungeGroupsSplitFourAndFourWithTheMarksApart()
    {
        foreach (var first in AllRoles)
        foreach (var second in AllRoles.Where(r => r != first))
        {
            var (away, behind) = DsrP2ThordanState.PlungeGroups(first, second);
            Assert.That(away.Concat(behind), Is.EquivalentTo(AllRoles));
            Assert.That(away, Has.Count.EqualTo(4));
            Assert.That(away, Does.Contain(first));
            Assert.That(behind, Does.Contain(second));
        }
    }

    [Test]
    public void TwoSwordsInTheOneSwordPartySwapsWithItsPartner()
    {
        var (away, behind) = DsrP2ThordanState.PlungeGroups(MainTank, RegenHealer);

        Assert.That(away, Is.EquivalentTo(new[] { MainTank, ShieldHealer, MeleeDpsA, PhysRangedDps }));
        Assert.That(behind, Is.EquivalentTo(new[] { OffTank, RegenHealer, MeleeDpsB, CasterDps }));
    }

    [Test]
    public void PartnersShareARoleAcrossLightParties()
    {
        foreach (var role in AllRoles)
        {
            var partner = DsrP2ThordanState.Partner(role);
            Assert.That(DsrP2ThordanState.Partner(partner), Is.EqualTo(role));
            Assert.That(DsrP2ThordanState.InLightPartyOne(partner), Is.Not.EqualTo(DsrP2ThordanState.InLightPartyOne(role)));
            Assert.That(DsrP2ThordanState.IsSupport(partner), Is.EqualTo(DsrP2ThordanState.IsSupport(role)));
        }
    }

    [Test]
    public void MeteorsAreTwoSupportsOrTwoDps()
    {
        foreach (var state in Rolls())
        {
            Assert.That(state.MeteorTargets, Has.Count.EqualTo(2).And.Unique);
            Assert.That(DsrP2ThordanState.IsSupport(state.MeteorTargets[0]), Is.EqualTo(DsrP2ThordanState.IsSupport(state.MeteorTargets[1])));
        }
    }

    private static IEnumerable<PartyRole[]> MeteorPairs()
    {
        PartyRole[][] roleGroups = [[MainTank, OffTank, RegenHealer, ShieldHealer], [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps]];
        foreach (var group in roleGroups)
            for (var i = 0; i < group.Length; i++)
            for (var j = i + 1; j < group.Length; j++)
                yield return [group[i], group[j]];
    }

    [TestCaseSource(nameof(MeteorPairs))]
    public void MeteorsEndNorthAndSouthAndEveryPairKeepsASupportAndADps(PartyRole first, PartyRole second)
    {
        var state = new DsrP2ThordanState(new Rng(1), new DsrP2ThordanStateOverrides { MeteorTargets = [first, second] });

        var cardinals = state.MeteorCardinals();

        Assert.That(new[] { cardinals[first], cardinals[second] }, Is.EquivalentTo(new[] { 0f, 180f }));
        foreach (var cardinal in new[] { 0f, 90f, 180f, 270f })
        {
            var pair = cardinals.Where(kv => kv.Value == cardinal).Select(kv => kv.Key).ToList();
            Assert.That(pair, Has.Count.EqualTo(2), $"pair at {cardinal}");
            Assert.That(pair.Count(DsrP2ThordanState.IsSupport), Is.EqualTo(1), $"pair at {cardinal}");
        }
    }

    [Test]
    public void FirstTowersAreTwoPerQuadrant()
    {
        foreach (var state in Rolls())
        {
            var outer = state.FirstTowers.Where(t => t.Radius == DsrP2ThordanState.OuterTowerRadius).ToList();
            var inner = state.FirstTowers.Where(t => t.Radius == DsrP2ThordanState.InnerTowerRadius).ToList();
            Assert.That(state.FirstTowers, Has.Count.EqualTo(8));
            var singles = 0;
            foreach (var cardinal in new[] { 0f, 90f, 180f, 270f })
            {
                var count = outer.Count(t => MathF.Abs(DsrP2ThordanState.Normalize(t.Bearing - cardinal + 180f) - 180f) <= 30f);
                Assert.That(count, Is.InRange(1, 2));
                if (count == 1) singles++;
            }
            Assert.That(inner, Has.Count.EqualTo(singles));
        }
    }

    [Test]
    public void EveryFirstTowerGetsExactlyOneSoakerAndMeteorRolesTakeTheWall()
    {
        foreach (var state in Rolls())
        {
            var soaks = state.FirstTowerSoaks();
            Assert.That(soaks.Keys, Is.EquivalentTo(AllRoles));
            Assert.That(soaks.Values, Is.EquivalentTo(state.FirstTowers));
            foreach (var role in AllRoles.Where(state.IsMeteorRole))
                Assert.That(soaks[role].Radius, Is.EqualTo(DsrP2ThordanState.OuterTowerRadius), role.ToString());
        }
    }

    [Test]
    public void NorthAndSouthMeteorsTakeTowersStraightAcrossWhenTheyExist()
    {
        var state = new DsrP2ThordanState(new Rng(1), new DsrP2ThordanStateOverrides
        {
            MeteorTargets = [MainTank, OffTank],
            FirstTowers = [(330f, 18f), (30f, 18f), (180f, 18f), (150f, 18f), (90f, 18f), (135f, 6f), (270f, 18f), (315f, 6f)],
        });

        var soaks = state.FirstTowerSoaks();

        Assert.That(soaks[MainTank].Bearing, Is.EqualTo(330f));
        Assert.That(soaks[OffTank].Bearing, Is.EqualTo(150f));
        Assert.That(soaks[PhysRangedDps].Bearing, Is.EqualTo(30f));
        Assert.That(soaks[CasterDps].Bearing, Is.EqualTo(180f));
    }

    [Test]
    public void WithoutAnAcrossPairMeteorsKeepTheCardinalTower()
    {
        var state = new DsrP2ThordanState(new Rng(1), new DsrP2ThordanStateOverrides
        {
            MeteorTargets = [MainTank, OffTank],
            FirstTowers = [(0f, 18f), (30f, 18f), (180f, 18f), (150f, 18f), (90f, 18f), (135f, 6f), (270f, 18f), (315f, 6f)],
        });

        var soaks = state.FirstTowerSoaks();

        Assert.That(soaks[MainTank].Bearing, Is.EqualTo(0f));
        Assert.That(soaks[OffTank].Bearing, Is.EqualTo(180f));
    }

    [Test]
    public void NorthAndSouthMeteorTowersAreStraightAcrossWheneverTheRollAllowsIt()
    {
        foreach (var state in Rolls(500))
        {
            var soaks = state.FirstTowerSoaks();
            var cardinals = state.MeteorCardinals();
            var north = state.MeteorTargets.First(r => cardinals[r] == 0f);
            var south = state.MeteorTargets.First(r => cardinals[r] == 180f);
            var wall = state.FirstTowers.Where(t => t.Radius == DsrP2ThordanState.OuterTowerRadius).Select(t => t.Bearing).ToList();
            var possible = wall.Any(n => (n >= 330f || n <= 30f) && wall.Contains(DsrP2ThordanState.Normalize(n + 180f)));
            var across = DsrP2ThordanState.Normalize(soaks[north].Bearing - soaks[south].Bearing) == 180f;
            Assert.That(across, Is.EqualTo(possible));
        }
    }

    [Test]
    public void SecondTowersCoverEveryCardinalAndIntercardinalWithMeteorRolesOnCardinals()
    {
        foreach (var state in Rolls())
        {
            var bearings = AllRoles.ToDictionary(r => r, state.SecondTowerBearing);
            Assert.That(bearings.Values, Is.EquivalentTo(new[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f }));
            foreach (var role in AllRoles)
                Assert.That(bearings[role] % 90f == 0f, Is.EqualTo(state.IsMeteorRole(role)), role.ToString());
        }
    }

    [Test]
    public void ChargePathsMirrorThroughTheCentreAndFollowTheRotation()
    {
        var clockwise = new DsrP2ThordanState(new Rng(1), new DsrP2ThordanStateOverrides { SanctityClockwise = true });
        var counter = new DsrP2ThordanState(new Rng(1), new DsrP2ThordanStateOverrides { SanctityClockwise = false });

        Assert.That(clockwise.AdelphelChargePath()[0].X, Is.GreaterThan(0f));
        Assert.That(counter.AdelphelChargePath()[0].X, Is.LessThan(0f));
        for (var i = 0; i < 4; i++)
            Assert.That(clockwise.JanlenouxChargePath()[i], Is.EqualTo(-clockwise.AdelphelChargePath()[i]));
    }

    [TestCase(true, 45f, true)]
    [TestCase(true, 135f, false)]
    [TestCase(false, 135f, true)]
    [TestCase(false, 45f, false)]
    public void BrightsphereDodgeTimingDependsOnTheDarkKnightAndRotation(bool clockwise, float darkKnight, bool late)
    {
        var state = new DsrP2ThordanState(new Rng(1), new DsrP2ThordanStateOverrides { SanctityClockwise = clockwise, DarkKnightBearing = darkKnight });

        Assert.That(state.LateBrightsphereDodge, Is.EqualTo(late));
    }

    [Test]
    public void GazesComeFromTwoDifferentDirections()
    {
        foreach (var state in Rolls())
            Assert.That(state.EyeBearing, Is.Not.EqualTo(state.SanctityThordanBearing));
    }

    [Test]
    public void TheEyeLightsTheMapEffectSlotFacingItsBearing()
    {
        foreach (var state in Rolls())
            Assert.That(state.EyeSlot * 45f, Is.EqualTo(state.EyeBearing));
    }

    [Test]
    public void OverridesPinTheRolls()
    {
        var state = new DsrP2ThordanState(new Rng(7), new DsrP2ThordanStateOverrides
        {
            ThordanBearing = 90f, SafeLineBearing = 45f, GuerriqueBearing = 180f, DarkKnightBearing = 315f,
            FirstPlungeTarget = CasterDps, SecondPlungeTarget = MainTank,
        });

        Assert.That(state.ThordanBearing, Is.EqualTo(90f));
        Assert.That(state.SafeLineBearing, Is.EqualTo(45f));
        Assert.That(state.GuerriqueBearing, Is.EqualTo(180f));
        Assert.That(state.DarkKnightBearing, Is.EqualTo(315f));
        Assert.That(state.FirstPlungeTarget, Is.EqualTo(CasterDps));
        Assert.That(state.SecondPlungeTarget, Is.EqualTo(MainTank));
    }
}
