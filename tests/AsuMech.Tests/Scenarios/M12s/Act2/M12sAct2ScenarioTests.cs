using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios;
using AsuMech.Scenarios.M12s.Act2;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.M12s.M12sConstants;
using static AsuMech.Tests.NegativeRun;
using Scenario = AsuMech.Scenarios.M12s.Act2.M12sAct2Scenario;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M12sAct2ScenarioTests
{
    private const int Seed = 5;

    private static PartyRole PartnerOf(PartyRole player, M12sAct2StateOverrides overrides)
    {
        var state = new M12sAct2State(new Rng(Seed), overrides, player);
        var order = state.Order[player];
        return player.IsDps() ? state.Beta(order) : state.Alpha(order);
    }

    [Test]
    public void AChainLeftUnstretchedKillsBothEnds()
    {
        var overrides = new M12sAct2StateOverrides { PlayerOrder = 1 };
        Negative<Scenario>(MeleeDpsA)
            .Seed(Seed)
            .Overrides<M12sAct2StateOverrides>(o => o.PlayerOrder = 1)
            .TeleportAt(Scenario.ChainAt[0] + 0.05f, to: Vector2.Zero)
            .ShouldKill(ActionId.DramaticLysisChainFail, MeleeDpsA, PartnerOf(MeleeDpsA, overrides));
    }

    [Test]
    public void StayingInsideTheCoilForConstrictorKills()
        => Negative<Scenario>(ShieldHealer)
            .TeleportAt(Scenario.ConstrictorAt - 1f, to: Vector2.Zero)
            .ShouldKill(ActionId.ConstrictorKill, ShieldHealer);

    [Test]
    public void AnUnsoakedBlobTowerKillsWhoeverStillCarriesAVulnerability()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M12sAct2StateOverrides>(o => o.PlayerOrder = 3)
            .TeleportAt(Scenario.BlobTowerCastAt[0], to: Vector2.Zero)
            .ShouldKillSomeone(ActionId.UnmitigatedExplosion);

    private static IEnumerable<int> Seeds() => Enumerable.Range(0, 30);

    // Every tower soaked and nobody touched by the coil, until the player walks out of the arena.
    [TestCaseSource(nameof(Seeds))]
    public void TheStratSoaksEveryTowerAndAvoidsTheCoil(int seed)
        => Negative<Scenario>(CasterDps)
            .Seed(seed)
            .At(Scenario.SplattershedCastAt, p =>
            {
                foreach (var role in Enum.GetValues<PartyRole>())
                    Assert.That(p.Member(role)!.IsAlive(), Is.True, $"{role} died");
            })
            .TeleportAt(Scenario.SplattershedCastAt + 0.5f, to: new Vector2(0f, 18f))
            .ShouldKill(TheEnvironment, CasterDps);
}
