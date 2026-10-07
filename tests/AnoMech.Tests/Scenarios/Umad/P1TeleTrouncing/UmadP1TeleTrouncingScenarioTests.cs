using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Umad;
using AnoMech.Scenarios.Umad.P1TeleTrouncing;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Umad.P1TeleTrouncing.TelePortentDirection;
using static AnoMech.Tests.NegativeRun;
using ActionId = AnoMech.Scenarios.Umad.UmadConstants.ActionId;

namespace AnoMech.Tests;

// Every roll is pinned: DPS matching and Confused, supports different and asleep; the support
// Confetti stack on MT, the DPS one on M2; a truthful normal gaze, spread fire, and truthful
// thunder on slots 0 and 2, the diagonal bands through (10.6,10.6) and (-3.5,-3.5).
//
//   14.33  First arrows under M1 (-12,6) M2 (-6,-12) R (12,-6) C (6,12) MT (-12,-6) OT (6,-12)
//          RH (12,6) SH (-6,12); the second wave at 17.32 one slot on round the r=12 square.
//          Each arrow carries 6y onto the next, so the 16 make one clockwise ring.
//   22.78  Confetti: MT at (-6,-6) with OT RH SH at (-4,-4); M2 at (6,6) with M1 R C at (4,4).
//   28.46  Idyllic Will (5y) on MT (0,-7) OT (-7,0) RH (0,15) SH (15,0).
//   29.09  Confused M1 (0,7) M2 (7,0) R (0,-15) C (-15,0) chase the nearest sleeper; each rides
//          four arrows of the ring.
//   41.40  Thrumming Thunder; 41.49 the gaze from (5.25,-66); 42.19 Flagrant Fire, spread with
//          M1 at (0.7,1.7) and OT at (6.3,6.2), or the stacks on MT and M1.
//   44.69  Light of Judgment kills everyone if any arrow went unsoaked by a Confused player.
//
// A run pauses 5s after its first death, so only a test nobody dies in before 44.69 sees the
// Light of Judgment.
public class UmadP1TeleTrouncingScenarioTests
{
    private const float FacingSouth = 0f;
    private const float FacingNorth = MathF.PI;

    internal static void Pin(UmadP1TeleTrouncingStateOverrides o)
    {
        o.DpsGetsDifferent = false;
        o.DpsGetsConfused = true;
        o.MatchingDirections = [Up, Right, Down, Left];
        o.DifferentCycleRoles = [MainTank, OffTank, RegenHealer, ShieldHealer];
        o.DifferentPolarity = [false, false, false, false];
        o.ConfettiStackSupport = MainTank;
        o.ConfettiStackDps = MeleeDpsB;
        o.GazeInverted = false;
        o.FireIsStack = false;
        o.FireIsLie = false;
        o.FireStackSupport = MainTank;
        o.FireStackDps = MeleeDpsA;
        o.ThunderIsLie = false;
        o.ThunderOrientationFlipped = false;
        o.ThunderRealOffset = 0;
    }

    private static NegativeRun<UmadP1TeleTrouncingScenario> TeleTrouncing(PartyRole player, Action<UmadP1TeleTrouncingStateOverrides>? tweak = null)
        => Negative<UmadP1TeleTrouncingScenario>(player)
            .Overrides<UmadP1TeleTrouncingStateOverrides>(o =>
            {
                Pin(o);
                tweak?.Invoke(o);
            });

    // MT stays on its first arrow, so the second drops onto it and both vanish. C, carried off
    // M1's second arrow onto the gap, walks on to OT.
    [Test]
    public void CollidedArrowsLetConfusedPlayerThrough()
        => TeleTrouncing(MainTank)
            .FreezeAt(14.5f)
            .TeleportAt(21f, to: new(-6, -6))
            .TeleportAt(24f, to: new(0, -7))
            .ShouldKill(ActionId.ConfusedAttack, OffTank);

    // C takes the first arrow of its own ride 0.14s before Confused lands; the rest of the ride
    // goes as usual.
    [Test]
    public void UsingArrowWhileNotConfusedWipes()
        => TeleTrouncing(CasterDps)
            .TeleportAt(28.6f, to: new(-12, 0))
            .TeleportAt(36f, to: new(-18.3f, 0), facing: FacingSouth)
            .ShouldKill(UmadConstants.ActionId.LightOfJudgment_Enrage, PerRole.All);

    // C, Confused at the edge and pulled back there once mid-chase, reaches M2's first arrow
    // after it expires.
    [Test]
    public void UnsoakedArrowWipes()
        => TeleTrouncing(CasterDps)
            .TeleportAt(28.6f, to: new(-19.5f, 0))
            .TeleportAt(30f, to: new(-19.5f, 0))
            .TeleportAt(36f, to: new(-18.3f, 0), facing: FacingSouth)
            .ShouldKill(UmadConstants.ActionId.LightOfJudgment_Enrage, PerRole.All);

    // The holder dies with the two who stayed.
    [Test]
    public void ConfettiStackShortABodyKillsStackers()
        => TeleTrouncing(OffTank)
            .TeleportAt(21f, to: new(-10, 10))
            .ShouldKill(ActionId.DoubleTroubleTrapStack, MainTank, RegenHealer, ShieldHealer);

    // The holders 9.9y apart and M1 4.95y from each; the support stack of five survives.
    [Test]
    public void ConfettiStackOverlapKillsPlayerInBoth()
        => TeleTrouncing(MeleeDpsA)
            .MoveBotAt(21f, MainTank, to: new(-2, -5))
            .MoveBotAt(21f, MeleeDpsB, to: new(5, 2))
            .TeleportAt(21f, to: new(1.5f, -1.5f))
            .ShouldKill(ActionId.DoubleTroubleTrapStack, MeleeDpsA);

    // Idyllic Will's vuln makes M1's own Indulgent Will lethal.
    [Test]
    public void ClippingSleepersIdyllicWillDies()
        => TeleTrouncing(MeleeDpsA)
            .TeleportAt(28f, to: new(0, -4))
            .ShouldKill(ActionId.IndulgentWill, MeleeDpsA);

    [Test]
    public void SleepersStandingTogetherKillEachOther()
        => TeleTrouncing(OffTank)
            .TeleportAt(28f, to: new(0, -4.5f))
            .ShouldKill(ActionId.IdyllicWill, OffTank, MainTank);

    // Beside M1, who was chasing RH anyway, as Confused lands.
    [Test]
    public void StandingNextToConfusedPlayerGetsCaught()
        => TeleTrouncing(RegenHealer)
            .TeleportAt(28.6f, to: new(0, 8.3f))
            .ShouldKill(ActionId.ConfusedAttack, RegenHealer);


    // In the real slot-2 line through (-3.5,-3.5).
    [TestCase(false, ActionId.ThrummingThunderIII_Real)]
    [TestCase(true, ActionId.ThrummingThunderIII_FakeAnim)]
    public void DiesToThrummingThunder(bool lie, uint line)
        => TeleTrouncing(MainTank, o => o.ThunderIsLie = lie)
            .TeleportAt(40.5f, to: new(-3.5f, -3.5f))
            .ShouldKill(line, MainTank);

    [Test]
    public void DiesToIndolentWillFacingStatue()
        => TeleTrouncing(MainTank)
            .TeleportAt(40.5f, to: new(7.4f, -4.4f), facing: FacingNorth)
            .ShouldKill(ActionId.IndolentWill, MainTank);

    [Test]
    public void DiesToAveMariaFacingAway()
        => TeleTrouncing(MainTank, o => o.GazeInverted = true)
            .TeleportAt(40.5f, to: new(7.4f, -4.4f), facing: FacingSouth)
            .ShouldKill(ActionId.AveMaria, MainTank);

    [Test]
    public void FireSpreadOverlapKillsBoth()
        => TeleTrouncing(OffTank)
            .TeleportAt(41.6f, to: new(3, 3.5f))
            .ShouldKill(ActionId.FlagrantFireSpread, OffTank, MeleeDpsA);

    [Test]
    public void FireStackShortABodyKillsStack()
        => TeleTrouncing(OffTank, o => o.FireIsStack = true)
            .TeleportAt(41.6f, to: new(-3, 13))
            .ShouldKill(ActionId.FlagrantFireStack, MainTank, RegenHealer, ShieldHealer);

    // The lie shows the spread icon.
    [Test]
    public void FollowingLyingFireIconDies()
        => TeleTrouncing(OffTank, o =>
            {
                o.FireIsStack = true;
                o.FireIsLie = true;
            })
            .TeleportAt(41.6f, to: new(-3, 13))
            .ShouldKill(ActionId.FlagrantFireStack, MainTank, RegenHealer, ShieldHealer);

    [Test]
    public void DiesWalkingOffArena()
        => TeleTrouncing(MainTank)
            .TeleportAt(3f, to: new(0, 20.5f))
            .ShouldKill(TheEnvironment, MainTank);
}
