using System.Linq;
using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Dsr.P3Nidhogg;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Dsr.DsrConstants;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class DsrP3NidhoggScenarioTests
{
    private static readonly PartyRole[] Everyone =
        [MainTank, OffTank, RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];

    // Line 1: MT, OT, H1 (all circles); line 2: H2, M1; line 3: M2, R, C.
    private static void PinnedDives(DsrP3NidhoggStateOverrides o)
    {
        o.Lines = new Dictionary<PartyRole, int>
        {
            [MainTank] = 1, [OffTank] = 1, [RegenHealer] = 1,
            [ShieldHealer] = 2, [MeleeDpsA] = 2,
            [MeleeDpsB] = 3, [PhysRangedDps] = 3, [CasterDps] = 3,
        };
        o.Arrows = Everyone.ToDictionary(r => r, _ => DiveArrow.Circle);
        o.LashFirst = [true, true];
    }

    [Test]
    public void LeavingADiveTowerUnsoakedWipes()
        => Negative<DsrP3NidhoggScenario>(MeleeDpsB)
            .Overrides<DsrP3NidhoggStateOverrides>(PinnedDives)
            .TeleportAt(41.5f, to: new Vector2(0f, -15f))
            .ShouldKill(ActionId.DarkdragonDiveFailed, Everyone);

    [Test]
    public void SoakingATowerRightAfterYourDiveIsLethal()
        => Negative<DsrP3NidhoggScenario>(MainTank)
            .Overrides<DsrP3NidhoggStateOverrides>(PinnedDives)
            .TeleportAt(42.5f, to: new Vector2(8f, 0f))
            .ShouldKill(ActionId.DarkdragonDive, MainTank);

    [Test]
    public void StandingOutsideTheDonutDiesToLashingWheel()
        => Negative<DsrP3NidhoggScenario>(ShieldHealer)
            .Overrides<DsrP3NidhoggStateOverrides>(PinnedDives)
            .TeleportAt(39.5f, to: new Vector2(-12f, -12f))
            .ShouldKill(ActionId.LashingWheel, ShieldHealer);

    // The divers only come out once their towers land, then warp away after their Geirskogul.
    [Test]
    public void DiversStayHiddenUntilTheirTowersLand()
        => Negative<DsrP3NidhoggScenario>(ShieldHealer)
            .Overrides<DsrP3NidhoggStateOverrides>(PinnedDives)
            .At(42.5f, p => Assert.That(VisibleDivers(p), Is.Zero))
            .At(44.5f, p => Assert.That(VisibleDivers(p), Is.EqualTo(3)))
            .At(54.5f, p => Assert.That(VisibleDivers(p), Is.EqualTo(2)))
            .At(82.0f, p => Assert.That(VisibleDivers(p), Is.Zero))
            .At(85.0f, p => Assert.That(VisibleDivers(p), Is.EqualTo(4)))
            .At(94.5f, p => Assert.That(VisibleDivers(p), Is.Zero))
            .TeleportAt(113f, to: new Vector2(0f, 6f))
            .ShouldKill(ActionId.Drachenlance, ShieldHealer);

    private static int VisibleDivers(ScenarioProbe p) =>
        p.World.Children.OfType<SimEnemy>().Count(e => e.IsActive && e.Visible && e.SpawnConfig.BNpcBaseId == BNpcBaseId.NidhoggDiver);

    [Test]
    public void StandingBesideATankHoldingSoulTetherIsLethal()
        => Negative<DsrP3NidhoggScenario>(RegenHealer)
            .Overrides<DsrP3NidhoggStateOverrides>(o =>
            {
                PinnedDives(o);
                o.TowerSoakers = [2, 2, 2, 2];
                o.SoulTetherTargets = [CasterDps, PhysRangedDps];
            })
            .TeleportAt(90.5f, to: Vector2.Zero)
            .ShouldKill(ActionId.SoulTether, RegenHealer);
}
