using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Umad.P2Forsaken;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Tests.NegativeRun;
using ActionId = AnoMech.Scenarios.Umad.UmadConstants.ActionId;
using LockonId = AnoMech.Scenarios.Umad.UmadConstants.LockonId;

namespace AnoMech.Tests;

// Every roll is pinned: north N turning clockwise one eighth per tower, supports Chariot and DPS
// Cone with the stacks on MT and M1, Future Past Future Past, and each tower's new lockons dealt
// in role order. MT RH M1 R soak towers 0 1 2 7, OT SH M2 C towers 3 to 6.
//
//   22.66  Tower 0 at (-5.7,-5.7) RH M1 and (5.7,-5.7) MT R; 23.26 RH's chariot at (-5.4,-8.5),
//          M1's stack at (-5.4,-2.4) with M2 C, MT's at (4.8,-4.8) with OT R, R's cone onto SH.
//   32.31  Future's End on the closest, MT (-1.8,-5.0); 32.35 the clones on RH (5.0,1.8),
//          OT (-1.0,5.9), M2 (-5.9,1.0).
//   32.67  Tower 1 at (0,-8) MT R and (8,0) RH M1; 33.27 MT's cone onto C, RH's onto SH,
//          R's chariot at (2.1,-11.0), M1's at (11.0,-2.1).
//   38.41  All Things Ending turns to the player, who baits Future's End at (-11,0) and Past's End
//          at (9.7,0); each later End's bait turns another quarter clockwise. The cones land 5s on.
//   Later towers at 43.71 53.72 64.76 74.77 85.81 95.82.
//
// A run pauses 5s after its first death.
public class UmadP2ForsakenScenarioTests
{
    internal static void Pin(UmadP2ForsakenStateOverrides o)
    {
        o.NewNorth = Direction.N;
        o.Rotation = 1;
        o.SupportLockon = LockonId.ForsakenChariot;
        o.SupportStackRole = MainTank;
        o.DpsStackRole = MeleeDpsA;
        o.EndAttacks[0] = EndAttack.FuturesEnd;
        o.EndAttacks[1] = EndAttack.PastsEnd;
        o.EndAttacks[2] = EndAttack.FuturesEnd;
        o.EndAttacks[3] = EndAttack.PastsEnd;
        o.ReassignLockonsInRoleOrder = true;
    }

    private static NegativeRun<UmadP2ForsakenScenario> Forsaken(PartyRole player, Action<UmadP2ForsakenStateOverrides>? tweak = null)
        => Negative<UmadP2ForsakenScenario>(player)
            .Overrides<UmadP2ForsakenStateOverrides>(o =>
            {
                Pin(o);
                tweak?.Invoke(o);
            });

    // A soaker steps out to Kefka just before its tower resolves.
    [TestCase(22.66f, MainTank)]
    [TestCase(32.67f, MainTank)]
    [TestCase(43.71f, MainTank)]
    [TestCase(53.72f, OffTank)]
    [TestCase(64.76f, OffTank)]
    [TestCase(74.77f, OffTank)]
    [TestCase(85.81f, OffTank)]
    [TestCase(95.82f, MainTank)]
    public void UnderfilledTowerWipes(float tower, PartyRole soaker)
        => Forsaken(soaker)
            .TeleportAt(tower - 0.1f, to: new(0, 0))
            .ShouldKill(ActionId.TheRiverOfLight, PerRole.All);

    [Test]
    public void OverfilledTowerWipes()
        => Forsaken(OffTank)
            .TeleportAt(22f, to: new(5, -6))
            .ShouldKill(ActionId.TheRiverOfLight, PerRole.All);

    [Test]
    public void EmptyTowerWipes()
        => Forsaken(RegenHealer)
            .TeleportAt(22f, to: new(0, 4))
            .MoveBotAt(22f, MeleeDpsA, to: new(-1, 4))
            .ShouldKill(ActionId.TheRiverOfLight, PerRole.All);

    [Test]
    public void StackShortABodyKillsStackers()
        => Forsaken(CasterDps)
            .TeleportAt(22.8f, to: new(0, 4))
            .ShouldKill(ActionId.Spelldriver, MeleeDpsA, MeleeDpsB);

    // M1 soaks from the inner edge of its tower, 6.8y from MT; OT stands between the two stacks.
    [Test]
    public void StandingInTwoStacksKillsPlayer()
        => Forsaken(OffTank)
            .MoveBotAt(22f, MeleeDpsA, to: new(-2, -4.5f))
            .TeleportAt(22.8f, to: new(1.4f, -4.65f))
            .ShouldKill(ActionId.Spelldriver, OffTank);

    // RH's chariot lands just before M1's stack.
    [Test]
    public void ClippedByChariotThenStackDies()
        => Forsaken(CasterDps)
            .TeleportAt(22.8f, to: new(-4.5f, -4))
            .ShouldKill(ActionId.Spelldriver, CasterDps);

    // RH soaks its tower beside M1, so each stands in the other's AoE.
    [Test]
    public void ChariotOverlappingStackKillsBoth()
        => Forsaken(RegenHealer)
            .TeleportAt(22f, to: new(-7, -6.5f))
            .ShouldKill(ActionId.Spelldriver, RegenHealer, MeleeDpsA);

    // OT, still vulnerable from its clone's Future's End, steps closer to RH than SH as tower 1
    // resolves and draws RH's cone.
    [Test]
    public void ClosestToConeHolderWhileVulnerableDies()
        => Forsaken(OffTank)
            .TeleportAt(32.5f, to: new(4.95f, 4.5f))
            .ShouldKill(ActionId.Spellwave, OffTank);

    // SH stays far enough out not to be one of the four targets, but within 5y of MT and M2.
    [Test]
    public void StandingInTwoEndCirclesDies()
        => Forsaken(ShieldHealer)
            .TeleportAt(32f, to: new(-6.2f, -3.6f))
            .ShouldKill(ActionId.FutureSEnd_CloneResolve, ShieldHealer);

    // MT takes Future's End, soaks tower 1, then steps into R's chariot. MT's cone now has R
    // closest, so R takes it on top of its own chariot.
    [Test]
    public void EndTargetWalkingIntoChariotDies()
        => Forsaken(MainTank)
            .TeleportAt(32.8f, to: new(1, -8))
            .ShouldKill(ActionId.Spellscatter, MainTank, PhysRangedDps);

    [Test]
    public void TwoEndTargetsStackedKillEachOther()
        => Forsaken(OffTank)
            .TeleportAt(32f, to: new(-5, 2))
            .ShouldKill(ActionId.FutureSEnd_CloneResolve, OffTank, MeleeDpsB);

    // The player stays where it baited Future's End as Kefka turns.
    [TestCase(0, 38.1f)]
    [TestCase(1, 59f)]
    [TestCase(2, 79.9f)]
    [TestCase(3, 100.9f)]
    public void StayingPutThroughFuturesEndDies(int occurrence, float freezeAt)
        => Forsaken(MainTank, o => o.EndAttacks[occurrence] = EndAttack.FuturesEnd)
            .FreezeAt(freezeAt)
            .ShouldKill(ActionId.AllThingsEnding_Future, MainTank);

    // Once Kefka has turned, the player crosses to its own side of the bait.
    [TestCase(0, 38.7f, -9.7f, 0f)]
    [TestCase(1, 59.6f, 0f, -9.7f)]
    [TestCase(2, 80.5f, 9.7f, 0f)]
    [TestCase(3, 101.5f, 6.86f, 6.86f)]
    public void CrossingPastKefkaThroughPastsEndDies(int occurrence, float at, float x, float y)
        => Forsaken(MainTank, o => o.EndAttacks[occurrence] = EndAttack.PastsEnd)
            .TeleportAt(at, to: new(x, y))
            .ShouldKill(ActionId.AllThingsEnding_Past, MainTank);
}
