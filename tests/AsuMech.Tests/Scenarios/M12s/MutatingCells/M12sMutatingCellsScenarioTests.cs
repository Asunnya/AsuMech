using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios;
using AsuMech.Scenarios.M12s.MutatingCells;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.M12s.M12sConstants;
using static AsuMech.Tests.NegativeRun;
using Scenario = AsuMech.Scenarios.M12s.MutatingCells.M12sMutatingCellsScenario;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M12sMutatingCellsScenarioTests
{
    [Test]
    public void EachMutationGoesToATankAHealerAndTwoDps()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var state = new M12sMutatingCellsState(new Rng(seed));
            Assert.That(state.AlphaFirst.Count(r => r.IsTank()), Is.EqualTo(1));
            Assert.That(state.AlphaFirst.Count(r => r is RegenHealer or ShieldHealer), Is.EqualTo(1));
            Assert.That(state.AlphaFirst.Count(r => r.IsDps()), Is.EqualTo(2));
        }
    }

    [Test]
    public void ThePlayerGetsTheMutationPicked()
    {
        foreach (var player in Enum.GetValues<PartyRole>())
        foreach (var alpha in new[] { true, false })
        {
            var state = new M12sMutatingCellsState(new Rng((int)player), new M12sMutatingCellsStateOverrides { PlayerAlphaFirst = alpha }, player);
            Assert.That(state.IsAlpha(player, 0), Is.EqualTo(alpha));
            Assert.That(state.IsAlpha(player, 1), Is.EqualTo(!alpha));
            Assert.That(state.AlphaFirst.Count(r => r.IsTank()), Is.EqualTo(1));
            Assert.That(state.AlphaFirst.Count(r => r.IsDps()), Is.EqualTo(2));
        }
    }

    [Test]
    public void EachSideHasOneOrbOfEachShapeAndOnlyTheCloseSideTwoNearItsHole()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var state = new M12sMutatingCellsState(new Rng(seed));
            foreach (var side in new[] { -1, 1 })
            {
                var mine = state.Orbs.Where(o => o.Side == side).ToList();
                Assert.That(mine.Select(o => o.Shape), Is.EquivalentTo(Enum.GetValues<OrbShape>()));
                var near = mine.Where(o => Vector3.Distance(o.Destination, M12sMutatingCellsState.Hole(side)) < 8f).ToList();
                Assert.That(near.Count, Is.EqualTo(side == state.CloseSide ? 2 : 0));
                if (side == state.CloseSide)
                    Assert.That(near.Select(o => o.Shape), Is.EquivalentTo(M12sMutatingCellsState.ClosePair(state.CloseWaterThunder)));
            }
            Assert.That(state.OrbsToPop.Count(), Is.EqualTo(2));
        }
    }

    [Test]
    public void ABetaWhoPopsNoOrbExplodes()
        => Negative<Scenario>(MainTank)
            .Overrides<M12sMutatingCellsStateOverrides>(o => o.PlayerAlphaFirst = false)
            .FreezeAt(Scenario.OrbsFlyOutAt)
            .ShouldKill(ActionId.UnmitigatedExplosion, MainTank);

    [Test]
    public void AnAlphaInTheNetherworldStackExplodes()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M12sMutatingCellsStateOverrides>(o => { o.PlayerAlphaFirst = false; o.NetherworldFar = false; })
            .TeleportAt(Scenario.WaveAt[1] + 1f, to: new Vector2(0f, 2.5f))
            .ShouldKill(ActionId.UnmitigatedExplosion, MeleeDpsA);

    // The far hole's fire reaches across the arena; only its own hole's shadow is safe.
    [Test]
    public void WaitingUnderTheWaterHoleInTheFirstWaveKills()
        => Negative<Scenario>(RegenHealer)
            .Overrides<M12sMutatingCellsStateOverrides>(o => { o.CloseSide = 1; o.CloseWaterThunder = true; })
            .TeleportAt(Scenario.WaveAt[0] - 1f, to: new Vector2(13f, 0f))
            .ShouldKill(ActionId.SidewaysFireII, RegenHealer);

    [Test]
    public void StandingInFrontOfDoubleSobatKills()
        => Negative<Scenario>(RegenHealer)
            .Overrides<M12sMutatingCellsStateOverrides>(o => o.SobatTurn = 0)
            .TeleportAt(Scenario.SobatBusterAt - 1f, to: new Vector2(0f, -7f))
            .ShouldKill(ActionId.DoubleSobatBuster, RegenHealer);

    [Test]
    public void TheTurnedRepeatCatchesWhoStaysBehind()
        => Negative<Scenario>(CasterDps)
            .Overrides<M12sMutatingCellsStateOverrides>(o => o.SobatTurn = 2)
            .FreezeAt(Scenario.SobatBusterAt + 0.1f)
            .ShouldKill(ActionId.DoubleSobatRepeat, CasterDps);

    private static IEnumerable<TestCaseData> Layouts()
        => from side in new[] { -1, 1 }
           from waterThunder in new[] { true, false }
           from far in new[] { true, false }
           select new TestCaseData(side, waterThunder, far);

    [TestCaseSource(nameof(Layouts))]
    public void EveryLayoutClears(int side, bool waterThunder, bool far)
        => Negative<Scenario>(OffTank)
            .Overrides<M12sMutatingCellsStateOverrides>(o => { o.CloseSide = side; o.CloseWaterThunder = waterThunder; o.NetherworldFar = far; })
            .At(Scenario.EsotericAt + 1f, p =>
            {
                foreach (var role in Enum.GetValues<PartyRole>())
                    Assert.That(p.Member(role)!.IsAlive(), Is.True, $"{role} died");
            })
            .TeleportAt(Scenario.EsotericAt + 1.2f, to: new Vector2(0f, 25f))
            .ShouldKill(TheEnvironment, OffTank);
}
