using System.Numerics;
using AnoMech.Scenarios.Dsr.P5Death;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Dsr.DsrConstants;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class DsrP5DeathScenarioTests
{
    private const float D = DsrP5DeathScenario.Death;

    // Warrior north. Conga line west to east: H1 M1 R MT OT C M2 H2; dooms M1 (W), MT (NW), OT (NE), M2 (E);
    // non-dooms H1 (W wall), R (SW), C (SE), H2 (E wall).
    private static void Pinned(DsrP5DeathStateOverrides o)
    {
        o.WarriorBearing = 0f;
        o.Dooms = [MeleeDpsA, OffTank, MainTank, MeleeDpsB];
        o.ThordanBearing = 90f;
        o.EyeBearing = 225f;
        o.FirstDoomSquare = true;
    }

    // The doom is put back after the puddle cleansed it, as if no puddle had been reached.
    [Test]
    public void DoomStillOnWhenItRunsOutKills()
        => Negative<DsrP5DeathScenario>(MeleeDpsB)
            .Overrides<DsrP5DeathStateOverrides>(Pinned)
            .At(D + 35.5f, p => p.Member(MeleeDpsB)!.AddStatus(StatusId.Doom, 2.3f))
            .ShouldKill(ActionId.Deathstorm, MeleeDpsB);

    [Test]
    public void ThePlayerLinesUpAtTheChosenCongaSpot()
        => Negative<DsrP5DeathScenario>(MeleeDpsB)
            .Overrides<DsrP5DeathStateOverrides>(o => { Pinned(o); o.PlayerCongaSlot = 0; })
            .At(D + 11.5f, p => Assert.That(Enumerable.Range(0, 8).Select(i => p.World.Party.Get(i)!).Where(m => m != p.Member(MeleeDpsB)).Min(m => m.Position.X), Is.GreaterThan(p.Member(MeleeDpsB)!.Position.X)))
            .At(D + 35.5f, p => p.Member(MeleeDpsB)!.AddStatus(StatusId.Doom, 2.3f))
            .ShouldKill(ActionId.Deathstorm, MeleeDpsB);

    [Test]
    public void DoomsHoldTheirStepBeforeSpreading()
        => Negative<DsrP5DeathScenario>(MeleeDpsB)
            .Overrides<DsrP5DeathStateOverrides>(Pinned)
            .At(D + 13.3f, p => Assert.That(MathF.Abs(p.Member(MeleeDpsB)!.Position.Z - p.Member(CasterDps)!.Position.Z), Is.InRange(2.5f, 3.5f)))
            .At(D + 35.5f, p => p.Member(MeleeDpsB)!.AddStatus(StatusId.Doom, 2.3f))
            .ShouldKill(ActionId.Deathstorm, MeleeDpsB);

    [Test]
    public void DoomsAreCleansedByTheKnockbackIntoPuddles()
        => Negative<DsrP5DeathScenario>(MeleeDpsB)
            .Overrides<DsrP5DeathStateOverrides>(Pinned)
            .At(D + 20f, p => Assert.That(Enumerable.Range(0, 8).Select(i => p.World.Party.Get(i)).Count(m => m != null && m.HasStatus(StatusId.Doom)), Is.EqualTo(4)))
            .At(D + 36.5f, p => Assert.That(Enumerable.Range(0, 8).Select(i => p.World.Party.Get(i)).Count(m => m != null && m.HasStatus(StatusId.Doom)), Is.Zero))
            .At(D + 36.6f, p => p.Member(MeleeDpsB)!.AddStatus(StatusId.Doom, 1.1f))
            .ShouldKill(ActionId.Deathstorm, MeleeDpsB);

    [Test]
    public void SharingLightningKillsBoth()
        => Negative<DsrP5DeathScenario>(CasterDps)
            .Overrides<DsrP5DeathStateOverrides>(Pinned)
            .TeleportAt(D + 20.5f, to: new Vector2(-19.5f, 0.8f))
            .ShouldKill(ActionId.LightningStormHit, CasterDps, RegenHealer);

    [Test]
    public void StayingOnYourTwisterKills()
        => Negative<DsrP5DeathScenario>(PhysRangedDps)
            .Overrides<DsrP5DeathStateOverrides>(Pinned)
            .FreezeAt(D + 21.0f)
            .ShouldKill(ActionId.Twister, PhysRangedDps);

    [Test]
    public void WaitingInAnUnexplodedRingKills()
        => Negative<DsrP5DeathScenario>(MeleeDpsB)
            .Overrides<DsrP5DeathStateOverrides>(Pinned)
            .TeleportAt(D + 21.3f, to: new Vector2(16f, 0f))
            .ShouldKill(ActionId.HeavyImpact, MeleeDpsB);

    [Test]
    public void StandingInTheMiddleDiesToCauterize()
        => Negative<DsrP5DeathScenario>(RegenHealer)
            .Overrides<DsrP5DeathStateOverrides>(Pinned)
            .TeleportAt(D + 20.0f, to: new Vector2(0f, 8f))
            .ShouldKill(ActionId.CauterizeDarkscale, RegenHealer);
}
