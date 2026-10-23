using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;
using AnoMech.Scenarios.M11s.ArenaSplit;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.M11s.M11sConstants;
using static AnoMech.Tests.NegativeRun;
using Scenario = AnoMech.Scenarios.M11s.ArenaSplit.M11sArenaSplitScenario;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M11sArenaSplitScenarioTests
{
    [Test]
    public void EachRoundTethersFourAndTheSecondTheOtherFour()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var state = new M11sArenaSplitState(new Rng(seed));
            var first = state.Tethers[0].Select(t => t.Role).ToList();
            var second = state.Tethers[1].Select(t => t.Role).ToList();
            Assert.That(first.Concat(second), Is.EquivalentTo(Enum.GetValues<PartyRole>()));
            foreach (var round in state.Tethers)
            {
                Assert.That(round.Count(t => t.Side < 0), Is.EqualTo(2));
                Assert.That(round.Where(t => t.Side < 0).Sum(t => t.Z), Is.EqualTo(0f));
            }
            var healers = state.Tethers[1].Where(t => t.Role is RegenHealer or ShieldHealer).ToList();
            if (healers.Count == 2) Assert.That(healers[0].Side, Is.Not.EqualTo(healers[1].Side));
            Assert.That(state.WestShadesNear[0], Is.Not.EqualTo(state.WestShadesNear[1]));
        }
    }

    [Test]
    public void ThePlayerIsTetheredInTheRoundTheyPicked()
    {
        foreach (var player in Enum.GetValues<PartyRole>())
        foreach (var round in new[] { 1, 2 })
        {
            var state = new M11sArenaSplitState(new Rng(7), new M11sArenaSplitStateOverrides { PlayerTetherRound = round }, player);
            Assert.That(state.TetherOf(round - 1, player), Is.Not.Null);
        }
    }

    [Test]
    public void TheSmashSparesOnlyTheFarCorner()
    {
        for (var avalanche = 0; avalanche < 4; avalanche++)
        {
            var state = new M11sArenaSplitState(new Rng(0), new M11sArenaSplitStateOverrides { Avalanche = avalanche });
            var corner = new Vector2(state.AvalancheSafeSide * 24f, state.AvalancheSafeZ * 18.5f);
            var other = new Vector2(state.AvalancheSafeSide * 24f, -state.AvalancheSafeZ * 18.5f);
            Assert.That(Scenario.InSmash(avalanche, corner), Is.False);
            Assert.That(Scenario.InSmash(avalanche, other), Is.True);
            Assert.That(Scenario.InSmash(avalanche, new Vector2(state.AvalancheSafeSide * 16f, 0f)), Is.True);
        }
    }

    [Test]
    public void StandingNorthOfTheBossForFlatlinerThrowsYouOff()
        => Negative<Scenario>(MainTank)
            .TeleportAt(Scenario.FlatlinerCastAt + 1f, to: new Vector2(0f, -8f))
            .ShouldKill(TheEnvironment, MainTank);

    [Test]
    public void AnUnstretchedTetherKillsItsHolder()
        => Negative<Scenario>(MainTank)
            .Seed(1)
            .Overrides<M11sArenaSplitStateOverrides>(o => { o.PlayerTetherRound = 1; o.FirstLinesWestOuter = true; })
            .TeleportAt(Scenario.BreathHitsAt[0] - 1f, to: new Vector2(-10f, 0f))
            .ShouldKill(ActionId.MajesticMeteowrath, MainTank);

    [Test]
    public void TakingMassiveMeteorAloneKills()
        => Negative<Scenario>(RegenHealer)
            .TeleportAt(Scenario.MassiveHitAt[0] - 1f, to: new Vector2(-16f, -18f))
            .MoveBotAt(Scenario.MassiveHitAt[0] - 1f, ShieldHealer, new Vector2(16f, -18f))
            .ShouldKill(ActionId.MassiveMeteorStack, RegenHealer, ShieldHealer);

    private static IEnumerable<int> Seeds() => Enumerable.Range(0, 40);

    // The player steps onto the falling platform at the end; until then nobody may take a line,
    // a puddle or an unsoaked tower.
    [TestCaseSource(nameof(Seeds))]
    public void TheStratClearsEveryTowerLineAndPuddle(int seed)
        => Negative<Scenario>(MeleeDpsA)
            .Seed(seed)
            .Overrides<M11sArenaSplitStateOverrides>(o => o.Avalanche = 3)
            .At(Scenario.AvalancheCastAt + Scenario.AvalancheResolves - 1f, p =>
            {
                foreach (var role in Enum.GetValues<PartyRole>())
                {
                    Assert.That(p.Member(role)!.HasStatus(StatusId.DamageDown), Is.False, $"{role} took an unsoaked tower");
                    Assert.That(p.Member(role)!.IsAlive(), Is.True, $"{role} died");
                }
            })
            .TeleportAt(Scenario.AvalancheCastAt + Scenario.AvalancheResolves - 0.5f, to: new Vector2(16f, 0f))
            .ShouldKill(TheEnvironment, MeleeDpsA);
}
