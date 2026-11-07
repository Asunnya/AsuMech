using System.Numerics;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Ucob.P3Heavensfall;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
// Nael lands north between Bahamut Prime (northwest) and Twintania (northeast).
public class UcobP3HeavensfallScenarioTests
{
    private static void NaelNorthBetween(UcobP3HeavensfallStateOverrides o)
    {
        o.NaelStep = 0;
        o.Formation = HeavensfallFormation.NaelBetween;
        o.BurstsClockwise = true;
    }

    // Nael northeast: Bahamut Prime north, Twintania east.
    private static void NaelNorthEastBetween(UcobP3HeavensfallStateOverrides o)
    {
        o.NaelStep = 1;
        o.Formation = HeavensfallFormation.NaelBetween;
        o.BurstsClockwise = true;
    }

    [Test]
    public void StandingInMegaflareDiveKills()
        => Negative<UcobP3HeavensfallScenario>(MeleeDpsA)
            .Overrides<UcobP3HeavensfallStateOverrides>(NaelNorthBetween)
            .TeleportAt(15.5f, to: new Vector2(7.07f, 7.07f))
            .ShouldKill(ActionId.MegaflareDive, MeleeDpsA);

    [Test]
    public void StayingOnYourTwisterSetsItOff()
        => Negative<UcobP3HeavensfallScenario>(CasterDps)
            .Overrides<UcobP3HeavensfallStateOverrides>(o =>
            {
                NaelNorthBetween(o);
                o.TwisterIncludes = CasterDps;
            })
            .FreezeAt(17f)
            .ShouldKill(ActionId.TwisterBlast, CasterDps);

    [Test]
    public void StayingInYourMegaflarePuddleKills()
        => Negative<UcobP3HeavensfallScenario>(CasterDps)
            .Overrides<UcobP3HeavensfallStateOverrides>(o =>
            {
                NaelNorthBetween(o);
                o.MegaflarePuddleIncludes = CasterDps;
            })
            .FreezeAt(19f)
            .ShouldKill(ActionId.MegaflarePuddle, CasterDps);

    [Test]
    public void LeavingYourTowerUnsoakedWipes()
        => Negative<UcobP3HeavensfallScenario>(CasterDps)
            .Overrides<UcobP3HeavensfallStateOverrides>(NaelNorthBetween)
            .TeleportAt(25.5f, to: new Vector2(0f, 0f))
            .ShouldKill(ActionId.MegaflareStrike, AllBut());

    [Test]
    public void StandingInTheFirstSliceKills()
        => Negative<UcobP3HeavensfallScenario>(CasterDps)
            .Overrides<UcobP3HeavensfallStateOverrides>(NaelNorthBetween)
            .TeleportAt(30.5f, to: new Vector2(7.07f, -7.07f))
            .ShouldKill(ActionId.ThermionicBurst, CasterDps);

    [Test]
    public void TakingTheFireballAloneKills()
        => Negative<UcobP3HeavensfallScenario>(CasterDps)
            .Overrides<UcobP3HeavensfallStateOverrides>(o =>
            {
                NaelNorthBetween(o);
                o.FireballTarget = CasterDps;
            })
            .TeleportAt(37f, to: new Vector2(10f, 0f))
            .ShouldKill(ActionId.Fireball, CasterDps);

    [Test]
    public void TheBotsClearEveryFormationAndSweep([Values] HeavensfallFormation formation, [Values] bool clockwise, [Range(0, 3)] int seed)
        => Negative<UcobP3HeavensfallScenario>(PhysRangedDps)
            .Overrides<UcobP3HeavensfallStateOverrides>(o =>
            {
                o.Formation = formation;
                o.BurstsClockwise = clockwise;
            })
            .Seed(seed)
            .ShouldKillNoOneElse();

    [Test]
    public void ADiveMarkerOffTheStackKillsOnlyItsTarget()
        => Negative<UcobP3HeavensfallScenario>(MeleeDpsA)
            .Overrides<UcobP3HeavensfallStateOverrides>(o =>
            {
                NaelNorthEastBetween(o);
                o.DiveIncludes = MeleeDpsA;
            })
            .TeleportAt(12.3f, to: new Vector2(5f, 0f))
            .ShouldKill(ActionId.MegaflareDive, MeleeDpsA);

    [Test]
    public void TheBotsSpreadOutOfADiveAimedOffTheStack([Range(0, 3)] int seed)
        => Negative<UcobP3HeavensfallScenario>(PhysRangedDps)
            .Overrides<UcobP3HeavensfallStateOverrides>(o =>
            {
                NaelNorthEastBetween(o);
                o.DiveIncludes = MeleeDpsA;
            })
            .MoveBotAt(12.3f, MeleeDpsA, new Vector2(5f, 0f))
            .Seed(seed)
            .ShouldKillNoOneElse();

    [Test]
    public void TheBotsBaitClearOfATwisterDroppedOnTheirSpot([Range(0, 3)] int seed)
        => Negative<UcobP3HeavensfallScenario>(PhysRangedDps)
            .Overrides<UcobP3HeavensfallStateOverrides>(o =>
            {
                NaelNorthBetween(o);
                o.TwisterIncludes = MeleeDpsA;
            })
            .MoveBotAt(17.3f, MeleeDpsA, UcobP3HeavensfallState.OnBearing(-10f, 13f))
            .Seed(seed)
            .ShouldKillNoOneElse();

    [Test]
    public void TheBotsKeepOutOfAPuddleDroppedInTheMiddle([Range(0, 3)] int seed)
        => Negative<UcobP3HeavensfallScenario>(PhysRangedDps)
            .Overrides<UcobP3HeavensfallStateOverrides>(o =>
            {
                NaelNorthBetween(o);
                o.MegaflarePuddleIncludes = MeleeDpsA;
            })
            .MoveBotAt(19.3f, MeleeDpsA, Vector2.Zero)
            .Seed(seed)
            .ShouldKillNoOneElse();

    [Test]
    public void TheTrioShowsWhereAndWhenTheyAreInTheFight()
    {
        var checks = 0;
        var run = ScenarioRun.Execute(typeof(UcobP3HeavensfallScenario), 0, 3, new ScenarioRunOptions
        {
            Overrides = o => NaelNorthBetween((UcobP3HeavensfallStateOverrides)o),
            Probe = p =>
            {
                var bosses = p.World.Children.OfType<SimEnemy>().ToList();
                SimEnemy Boss(uint baseId) => bosses.Single(e => e.SpawnConfig.BNpcBaseId == baseId);
                void Expect(float at, bool bahamut, bool nael, bool twintania)
                {
                    if (!p.Crossed(at)) return;
                    Assert.That(Boss(BNpcBaseId.BahamutPrime).Visible, Is.EqualTo(bahamut), $"Bahamut Prime at {at}");
                    Assert.That(Boss(BNpcBaseId.NaelDeusDarnus).Visible, Is.EqualTo(nael), $"Nael at {at}");
                    Assert.That(Boss(BNpcBaseId.Twintania).Visible, Is.EqualTo(twintania), $"Twintania at {at}");
                    checks++;
                }
                Expect(2f, true, false, false);
                Expect(UcobP3HeavensfallScenario.NaelAndTwintaniaShow + 0.1f, true, true, true);
                Expect(10.7f, false, false, false);
                Expect(UcobP3HeavensfallScenario.TrioLand + 0.1f, true, true, true);
                Expect(UcobP3HeavensfallScenario.TwintaniaDivesOff + 0.1f, false, true, false);
                Expect(UcobP3HeavensfallScenario.TwintaniaLands + 0.1f, false, true, true);
                Expect(UcobP3HeavensfallScenario.NaelGone + 0.1f, false, false, true);
                Expect(UcobP3HeavensfallScenario.BahamutLands + 0.1f, true, false, true);
                if (p.Crossed(UcobP3HeavensfallScenario.NaelAndTwintaniaShow + 0.1f))
                {
                    Assert.That(Flat(Boss(BNpcBaseId.NaelDeusDarnus).Position), Is.EqualTo(UcobP3HeavensfallScenario.NaelShowsAt));
                    Assert.That(Flat(Boss(BNpcBaseId.Twintania).Position), Is.EqualTo(UcobP3HeavensfallScenario.TwintaniaShowsAt));
                }
                if (p.Crossed(UcobP3HeavensfallScenario.TrioLand + 0.1f))
                    Assert.That(Vector2.Distance(Flat(Boss(BNpcBaseId.NaelDeusDarnus).Position), new Vector2(0f, -UcobP3HeavensfallState.NaelDistance)), Is.LessThan(0.01f));
            },
        });
        Assert.That(run.Passed, run.ToString());
        Assert.That(checks, Is.EqualTo(8));
    }

    private static Vector2 Flat(Vector3 at) => new(at.X, at.Z);
}
