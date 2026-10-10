using System.Numerics;
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

    [Test]
    public void StandingBesideATankHoldingSoulTetherIsLethal()
        => Negative<DsrP3NidhoggScenario>(RegenHealer)
            .Overrides<DsrP3NidhoggStateOverrides>(o =>
            {
                PinnedDives(o);
                o.TowerSoakers = [2, 2, 2, 2];
                o.SoulTetherTargets = [CasterDps, PhysRangedDps];
            })
            .TeleportAt(90.5f, to: new Vector2(0f, 10f))
            .ShouldKill(ActionId.SoulTether, RegenHealer);
}
