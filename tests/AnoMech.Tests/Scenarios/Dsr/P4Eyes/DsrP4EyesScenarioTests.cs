using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Dsr.P4Eyes;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Dsr.DsrConstants;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class DsrP4EyesScenarioTests
{
    // Every role starts on the colour the strat wants: melee and tanks red, ranged and healers blue.
    private static void RightColours(DsrP4EyesStateOverrides o)
    {
        o.SoulMarked = MainTank;
        o.FirstRed = [MainTank, OffTank, MeleeDpsA, MeleeDpsB];
        o.DivePicks = [(0, 1), (0, 1), (0, 1), (0, 1)];
    }

    [Test]
    public void MissingAlphinaudsBuffsIsLethal()
        => Negative<DsrP4EyesScenario>(PhysRangedDps)
            .Overrides<DsrP4EyesStateOverrides>(RightColours)
            .TeleportAt(15f, to: new Vector2(0f, -12f))
            .ShouldKill(ActionId.SoulOfFriendship, PhysRangedDps);

    [Test]
    public void TouchingAnOrbWithoutTheRedTetherIsLethal()
        => Negative<DsrP4EyesScenario>(RegenHealer)
            .Overrides<DsrP4EyesStateOverrides>(RightColours)
            .TeleportAt(38f, to: new Vector2(10f, -5.6f))
            .ShouldKill(ActionId.FlareStar, RegenHealer);

    [Test]
    public void LeavingABlueOrbUnpoppedKillsEveryoneAlreadyScorched()
        => Negative<DsrP4EyesScenario>(RegenHealer)
            .Overrides<DsrP4EyesStateOverrides>(RightColours)
            .FreezeAt(41f)
            .ShouldKill(ActionId.FlareStarUnpopped, AllBut(RegenHealer));

    // The healers are dived first; the off tank never comes to take the regen healer's red.
    [Test]
    public void KeepingRedAfterADiveIsLethalOnTheNextOne()
        => Negative<DsrP4EyesScenario>(OffTank)
            .Overrides<DsrP4EyesStateOverrides>(o =>
            {
                RightColours(o);
                o.DivePicks = [(0, 1), (1, 0), (0, 1), (0, 1)];
            })
            .FreezeAt(59f)
            .ShouldKill(ActionId.MirageDive, RegenHealer);
}
