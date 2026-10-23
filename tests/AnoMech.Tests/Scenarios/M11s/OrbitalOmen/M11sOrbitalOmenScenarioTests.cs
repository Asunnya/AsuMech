using System.Numerics;
using AnoMech.Scenarios;
using AnoMech.Scenarios.M11s.OrbitalOmen;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.M11s.M11sConstants;
using static AnoMech.Tests.NegativeRun;
using Scenario = AnoMech.Scenarios.M11s.OrbitalOmen.M11sOrbitalOmenScenario;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M11sOrbitalOmenScenarioTests
{
    private static readonly Vector2 InTheSecondLine = new(3.2f, -3.6f);

    [Test]
    public void EveryLineOrderRollsBothPortalsAsPermutations()
    {
        for (var seed = 0; seed < 200; seed++)
        {
            var state = new M11sOrbitalOmenState(new Rng(seed));
            Assert.That(state.NorthLines, Is.EquivalentTo(M11sOrbitalOmenState.Offsets));
            Assert.That(state.EastLines, Is.EquivalentTo(M11sOrbitalOmenState.Offsets));
        }
    }

    [Test]
    public void TheSecondPairAlwaysCrossesNextToTheBoss()
    {
        foreach (var pattern in M11sOrbitalOmenState.Patterns)
            Assert.That((Math.Abs(pattern[1].North), Math.Abs(pattern[1].East)), Is.EqualTo((5, 5)));
    }

    [TestCaseSource(nameof(PatternIndices))]
    public void EveryPatternIsClearedWithoutAHit(int pattern)
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M11sOrbitalOmenStateOverrides>(o => o.Pattern = pattern)
            .TeleportAt(Scenario.LineAt[1] - 0.5f, to: InTheSecondLine)
            .At(Scenario.PairCastAt[^1] + Scenario.PairResolves + 0.8f, p =>
            {
                foreach (var role in Enum.GetValues<AnoMech.Core.Game.Party.PartyRole>())
                    Assert.That(p.Member(role)!.HasStatus(StatusId.DamageDown), Is.False, $"{role} took a portal line or a cone");
            })
            .ShouldKill(ActionId.GreatWallOfFireLine, MeleeDpsA);

    private static IEnumerable<int> PatternIndices() => Enumerable.Range(0, M11sOrbitalOmenState.Patterns.Length);

    // The main tank stepping into the second line is the run's death; everyone else must come out
    // of Orbital Omen and Fire and Fury untouched.
    [TestCaseSource(nameof(Seeds))]
    public void TheStratDodgesEveryPortalLineAndCone(int seed)
        => Negative<Scenario>(MainTank)
            .Seed(seed)
            .TeleportAt(Scenario.LineAt[1] - 0.5f, to: InTheSecondLine)
            .At(Scenario.PairCastAt[^1] + Scenario.PairResolves + 0.8f, p =>
            {
                foreach (var role in Enum.GetValues<AnoMech.Core.Game.Party.PartyRole>().Where(r => r != MainTank))
                    Assert.That(p.Member(role)!.HasStatus(StatusId.DamageDown), Is.False, $"{role} took a portal line or a cone");
            })
            .ShouldKill(ActionId.GreatWallOfFireLine, MainTank);

    private static IEnumerable<int> Seeds() => Enumerable.Range(0, 40);

    [Test]
    public void APlayerInTheFirstLineDies()
        => Negative<Scenario>(RegenHealer)
            .TeleportAt(Scenario.LineAt[0] - 0.5f, to: new Vector2(0f, -3f))
            .ShouldKill(ActionId.GreatWallOfFireLine, RegenHealer);

    [Test]
    public void TheWallExplodesWhereTheFirstLineWent()
        => Negative<Scenario>(MeleeDpsA)
            .TeleportAt(Scenario.WallExplodesCastAt[0] + 1f, to: new Vector2(0f, -12f))
            .ShouldKill(ActionId.WallExplosion, MeleeDpsA);

    [Test]
    public void StayingOnTheFlankTakesALine()
        => Negative<Scenario>(MeleeDpsA)
            .FreezeAt(Scenario.FuryCastAt + Scenario.FuryResolves)
            .At(Scenario.PairCastAt[^1] + Scenario.PairResolves + 0.8f, p => Assert.That(p.Member(MeleeDpsA)!.HasStatus(StatusId.DamageDown), Is.True))
            .MoveBotAt(Scenario.LineAt[1] - 0.5f, MainTank, InTheSecondLine)
            .ShouldKill(ActionId.GreatWallOfFireLine, MainTank);
}
