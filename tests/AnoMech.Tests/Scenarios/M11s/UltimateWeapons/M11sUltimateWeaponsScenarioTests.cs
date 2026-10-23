using System.Numerics;
using AnoMech.Scenarios.M11s;
using AnoMech.Scenarios.M11s.UltimateWeapons;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.M11s.M11sConstants;
using static AnoMech.Tests.NegativeRun;
using Scenario = AnoMech.Scenarios.M11s.UltimateWeapons.M11sUltimateWeaponsScenario;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M11sUltimateWeaponsScenarioTests
{
    // Lines open south; tornadoes W (-8.9,-0.9), S (-1.4,12.1), E (10.6,0.1), N (0,-12).
    private static void Pinned(M11sUltimateWeaponsStateOverrides o)
    {
        o.OpenSide = 0;
        o.AxeTrophy = false;
        o.Mirrored = false;
        o.Weapons = [Weapon.Sword, Weapon.Scythe, Weapon.Axe, Weapon.Scythe, Weapon.Sword, Weapon.Axe];
    }

    [Test]
    public void APairInALineDiesThroughTheStacksVuln()
        => Negative<Scenario>(MainTank)
            .Overrides<M11sUltimateWeaponsStateOverrides>(Pinned)
            .TeleportAt(Scenario.EyeCastAt + 1f, to: new Vector2(-15f, 0f))
            .MoveBotAt(Scenario.EyeCastAt + 1f, MeleeDpsA, new Vector2(-15f, 0.3f))
            .ShouldKill(ActionId.HurricaneExplosion, MainTank, MeleeDpsA);

    [Test]
    public void TwoPairsOnOneSpotKillEachOther()
        => Negative<Scenario>(MainTank)
            .Overrides<M11sUltimateWeaponsStateOverrides>(Pinned)
            .TeleportAt(Scenario.EyeCastAt + 1f, to: new Vector2(9f, 0.3f))
            .ShouldKill(ActionId.EyeOfTheHurricane, MainTank, OffTank, MeleeDpsB);

    [Test]
    public void TornadoesLeaveDamageDownAndADoubledGustKills()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M11sUltimateWeaponsStateOverrides>(Pinned)
            .TeleportAt(Scenario.StrikeAt[^1] + 2.5f, to: new Vector2(-8.9f, -0.9f))
            .At(Scenario.StrikeAt[^1] + 3.5f, p => Assert.That(p.Member(MeleeDpsA)!.HasStatus(StatusId.DamageDown), Is.True))
            .TeleportAt(Scenario.GustAt - 0.5f, to: new Vector2(-13.1f, -4.3f))
            .ShouldKill(ActionId.PowerfulGust, MeleeDpsA, RegenHealer);

    private static IEnumerable<TestCaseData> EveryLayout()
        => from side in Enumerable.Range(0, 4)
           from mirrored in new[] { false, true }
           from seed in Enumerable.Range(0, 5)
           select new TestCaseData(side, mirrored, seed);

    // H1 baits the west tornado; a player on H1's spot doubles its gusts.
    [TestCaseSource(nameof(EveryLayout))]
    public void TheStratKeepsEveryoneOutOfTheLinesAndTornadoes(int side, bool mirrored, int seed)
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M11sUltimateWeaponsStateOverrides>(o => { o.OpenSide = side; o.Mirrored = mirrored; })
            .Seed(seed)
            .At(Scenario.GustAt - 1f, p =>
            {
                foreach (var role in Enum.GetValues<AnoMech.Core.Game.Party.PartyRole>())
                    Assert.That(p.Member(role)!.HasStatus(StatusId.DamageDown), Is.False, $"{role} took a line or a tornado");
            })
            .TeleportAt(Scenario.GustAt - 0.5f, to: mirrored ? new Vector2(-15.1f, -2.9f) : new Vector2(-13.1f, -4.3f))
            .ShouldKill(ActionId.PowerfulGust, MeleeDpsA, RegenHealer);

    [Test]
    public void LeavingTheLastScythesFootKills()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M11sUltimateWeaponsStateOverrides>(Pinned)
            .TeleportAt(Scenario.StrikeAt[3] + 0.2f, to: new Vector2(-1.4f, 4f))
            .ShouldKill(ActionId.AssaultEvolvedScythe, MeleeDpsA);
}
