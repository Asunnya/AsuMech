using System.Numerics;
using AnoMech.Scenarios.Dsr.P6Wroth;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Dsr.DsrConstants;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class DsrP6WrothScenarioTests
{
    // Fire on M1, H1 and the caster: pairs front M1+M2, west H1+R, east H2+C, no swap. Nidhogg glows,
    // so Hraesvelgr's Staggering Breath lands on the OT. North wing, tanks close, Nidhogg dives the
    // east half. Spreads M2 R H1 MT line up west to east; stacks C (near) + M1 and OT (wall) + H2.
    private static void Pinned(DsrP6WrothStateOverrides o)
    {
        o.NidhoggTethers = [MeleeDpsA, RegenHealer, CasterDps];
        o.Glow = BreathGlow.Nidhogg;
        o.VowTarget = PhysRangedDps;
        o.HraesvelgrStackOnRegen = true;
        o.WingsNorth = true;
        o.PlumesNear = true;
        o.NidhoggDivesEast = true;
        o.NidhoggFromSouth = true;
        o.Spreads = [MainTank, RegenHealer, MeleeDpsB, PhysRangedDps];
        o.Stacks = [OffTank, CasterDps];
        o.AkhMornTarget = MeleeDpsA;
        o.HraesvelgrLane = 0;
        o.HraesvelgrFromSouth = true;
        o.SecondSetCorner = (-1, 1);
        o.HotWing = true;
    }

    [Test]
    public void SlackBreathTetherKills()
        => Negative<DsrP6WrothScenario>(CasterDps)
            .Overrides<DsrP6WrothStateOverrides>(Pinned)
            .TeleportAt(18f, to: new Vector2(-8f, 2f))
            .ShouldKill(ActionId.FlameBreath, CasterDps);

    [Test]
    public void JoiningAnotherFirePairKillsTheWholePair()
        => Negative<DsrP6WrothScenario>(RegenHealer)
            .Overrides<DsrP6WrothStateOverrides>(Pinned)
            .TeleportAt(20f, to: new Vector2(0.2f, 7.7f))
            .ShouldKill(ActionId.FlameBreath, RegenHealer, MeleeDpsA, MeleeDpsB);

    [Test]
    public void TakingOnlyYourOwnBreathLeavesALethalDebuff()
        => Negative<DsrP6WrothScenario>(CasterDps)
            .Overrides<DsrP6WrothStateOverrides>(Pinned)
            .TeleportAt(20f, to: new Vector2(-4f, -13.5f))
            .At(22.5f, p => Assert.That(p.Member(CasterDps)!.HasStatus(StatusId.Boiling)))
            .ShouldKill(ActionId.FlameBreath, CasterDps)
            .ShouldKill(ActionId.IceBreath, ShieldHealer);

    [Test]
    public void PairsTakeBothElementsAndKeepNoDebuff()
        => Negative<DsrP6WrothScenario>(OffTank)
            .Overrides<DsrP6WrothStateOverrides>(Pinned)
            .At(22.5f, p =>
            {
                foreach (var role in new[] { RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps })
                {
                    var member = p.Member(role)!;
                    Assert.That(member.HasStatus(StatusId.FireResistanceDownII) && member.HasStatus(StatusId.IceResistanceDownII), role.ToString());
                    Assert.That(member.HasStatus(StatusId.Boiling) || member.HasStatus(StatusId.Freezing), Is.False, role.ToString());
                }
            })
            .TeleportAt(52f, to: new Vector2(-10f, -10f))
            .ShouldKill(ActionId.HallowedWingsNorth, OffTank);

    [Test]
    public void StandingInTheMiddleDiesToTheGlowingBreath()
        => Negative<DsrP6WrothScenario>(MeleeDpsA)
            .Overrides<DsrP6WrothStateOverrides>(Pinned)
            .TeleportAt(20f, to: new Vector2(1f, -1f))
            .ShouldKill(ActionId.DarkBreath, MeleeDpsA);

    [Test]
    public void StandingNearTheBustedTankKills()
        => Negative<DsrP6WrothScenario>(MeleeDpsB)
            .Overrides<DsrP6WrothStateOverrides>(Pinned)
            .TeleportAt(20f, to: new Vector2(2f, -14f))
            .ShouldKill(ActionId.StaggeringBreath, MeleeDpsB);

    [Test]
    public void TakingTheSharedOrbAloneKills()
        => Negative<DsrP6WrothScenario>(OffTank)
            .Overrides<DsrP6WrothStateOverrides>(o => { Pinned(o); o.Glow = BreathGlow.Both; })
            .TeleportAt(20f, to: new Vector2(10f, -9f))
            .ShouldKill(ActionId.HolyOrb, OffTank);

    [Test]
    public void LeavingTheDonutHoleKills()
        => Negative<DsrP6WrothScenario>(PhysRangedDps)
            .Overrides<DsrP6WrothStateOverrides>(Pinned)
            .TeleportAt(20f, to: new Vector2(-15f, -15f))
            .ShouldKill(ActionId.SwirlingBlizzard, PhysRangedDps);

    [Test]
    public void StandingNextToTheVowTargetKills()
        => Negative<DsrP6WrothScenario>(MeleeDpsA)
            .Overrides<DsrP6WrothStateOverrides>(Pinned)
            .TeleportAt(28f, to: new Vector2(-6f, 6f))
            .ShouldKill(ActionId.MortalVow, MeleeDpsA);

    [Test]
    public void TakingAkhAfahAloneKills()
        => Negative<DsrP6WrothScenario>(RegenHealer)
            .Overrides<DsrP6WrothStateOverrides>(Pinned)
            .TeleportAt(39.5f, to: new Vector2(10f, -5f))
            .ShouldKill(ActionId.AkhAfahHraesvelgrStack, RegenHealer);

    [Test]
    public void StandingUnderTheGlowingWingKills()
        => Negative<DsrP6WrothScenario>(MeleeDpsA)
            .Overrides<DsrP6WrothStateOverrides>(Pinned)
            .TeleportAt(53f, to: new Vector2(-10f, -10f))
            .ShouldKill(ActionId.HallowedWingsNorth, MeleeDpsA);

    [Test]
    public void StandingCloserThanTheTankWhenTheHeadIsDownKills()
        => Negative<DsrP6WrothScenario>(MeleeDpsB)
            .Overrides<DsrP6WrothStateOverrides>(Pinned)
            .TeleportAt(53.5f, to: new Vector2(-3f, 8f))
            .ShouldKill(ActionId.HallowedPlume, MeleeDpsB, MainTank);

    [Test]
    public void StandingInNidhoggsLaneKills()
        => Negative<DsrP6WrothScenario>(MeleeDpsB)
            .Overrides<DsrP6WrothStateOverrides>(o => { Pinned(o); o.PlumesNear = false; })
            .TeleportAt(53f, to: new Vector2(10f, 15f))
            .ShouldKill(ActionId.CauterizeNidhogg, MeleeDpsB);

    [Test]
    public void TankWalkingOffTheVowPassWipes()
        => Negative<DsrP6WrothScenario>(MainTank)
            .Overrides<DsrP6WrothStateOverrides>(Pinned)
            .TeleportAt(60f, to: new Vector2(10f, -10f))
            .ShouldKill(ActionId.MortalVowPass, AllBut());

    [Test]
    public void ASecondPlayerInTheVowPassKillsBoth()
        => Negative<DsrP6WrothScenario>(MeleeDpsA)
            .Overrides<DsrP6WrothStateOverrides>(Pinned)
            .TeleportAt(60f, to: new Vector2(0f, 0.5f))
            .ShouldKill(ActionId.MortalVowPass, MeleeDpsA, MainTank);

    [Test]
    public void MissingAkhMornWipes()
        => Negative<DsrP6WrothScenario>(CasterDps)
            .Overrides<DsrP6WrothStateOverrides>(Pinned)
            .TeleportAt(78f, to: new Vector2(10f, -12f))
            .ShouldKill(ActionId.AkhMorn, AllBut());

    [Test]
    public void StandingInHraesvelgrsLaneKills()
        => Negative<DsrP6WrothScenario>(CasterDps)
            .Overrides<DsrP6WrothStateOverrides>(Pinned)
            .TeleportAt(79.4f, to: new Vector2(0f, -10f))
            .ShouldKill(ActionId.CauterizeHraesvelgr, CasterDps);

    [Test]
    public void WaitingInTheThirdCornerDiesToItsCross()
        => Negative<DsrP6WrothScenario>(CasterDps)
            .Overrides<DsrP6WrothStateOverrides>(Pinned)
            .TeleportAt(84.2f, to: new Vector2(10f, -10f))
            .ShouldKill(ActionId.FlameBlast, CasterDps);

    [Test]
    public void StandingOnTheSidesDuringHotWingKills()
        => Negative<DsrP6WrothScenario>(MeleeDpsB)
            .Overrides<DsrP6WrothStateOverrides>(Pinned)
            .TeleportAt(91f, to: new Vector2(-13f, 10f))
            .ShouldKill(ActionId.HotWingSides, MeleeDpsB);

    [Test]
    public void OverlappingSpreadsKillBoth()
        => Negative<DsrP6WrothScenario>(MeleeDpsB)
            .Overrides<DsrP6WrothStateOverrides>(Pinned)
            .TeleportAt(92.5f, to: new Vector2(-14.5f, 0f))
            .ShouldKill(ActionId.SpreadingFlames, MeleeDpsB, PhysRangedDps);

    [Test]
    public void TakingEntangledFlamesAloneKills()
        => Negative<DsrP6WrothScenario>(CasterDps)
            .Overrides<DsrP6WrothStateOverrides>(Pinned)
            .TeleportAt(92.5f, to: new Vector2(4f, -2.5f))
            .ShouldKill(ActionId.EntangledFlames, CasterDps);
}
