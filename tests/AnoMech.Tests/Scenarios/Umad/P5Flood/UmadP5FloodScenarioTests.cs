using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P5Flood;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Tests.NegativeRun;
using ActionId = AnoMech.Scenarios.Umad.UmadConstants.ActionId;

namespace AnoMech.Tests;

// Pinned: NE-SW leads, both lines march Forward, the stack always on RH. Every lane is 40 x 10,
// running from its wall anchor at +-45 deg; the party clumps at r=4 off a cardinal, MT at the
// quadrant centre + (0,1.2), and only rotates after tick 1.
//
//   0.8    Party converges South.
//   6.45   Tick 0, NE-SW from (-10.6,-17.7) and (-24.7,-3.5): hits N and E.
//   7.47   Tick 1, NW-SE from (10.6,-17.7) and (24.7,-3.5): hits N and W.
//   7.57   Party rotates counter-clockwise to East.
//   8.49   Tick 2, NE-SW from (-3.5,-24.7) and (-17.7,-10.6): hits S and W.
//   8.59   Party rotates to North.
//   9.51   Tick 3, NW-SE from (3.5,-24.7) and (17.7,-10.6): hits E and S.
//
// A run pauses 5s after its first death.
public class UmadP5FloodScenarioTests
{
    private static void Pin(UmadP5FloodStateOverrides o)
    {
        o.NeSwFirst = true;
        o.LineNeSw = FloodDirection.Forward;
        o.LineNwSe = FloodDirection.Forward;
        o.AnchorRole = RegenHealer;
    }

    private static NegativeRun<UmadP5FloodScenario> Flood(PartyRole player, Action<UmadP5FloodStateOverrides>? tweak = null)
        => Negative<UmadP5FloodScenario>(player)
            .Overrides<UmadP5FloodStateOverrides>(o =>
            {
                Pin(o);
                tweak?.Invoke(o);
            });

    // 8y down the lane from its wall anchor, or 30y (past the centre).
    [TestCase(6.25f, -4.9f, -12f)]
    [TestCase(6.25f, 10.6f, 3.5f)]
    [TestCase(7.27f, 4.9f, -12f)]
    [TestCase(7.27f, -10.6f, 3.5f)]
    [TestCase(8.29f, -12f, -4.9f)]
    [TestCase(8.29f, 3.5f, 10.6f)]
    [TestCase(9.31f, 12f, -4.9f)]
    [TestCase(9.31f, -3.5f, 10.6f)]
    public void StandingInWaveLaneDies(float at, float x, float z)
        => Flood(MainTank)
            .TeleportAt(at, to: new(x, z))
            .ShouldKill(ActionId.FloodAOE, MainTank);

    // The opposite cardinal from the one the line rolls make safe; tick 0 hits it.
    [TestCase(false, false, 0f, -4f)]
    [TestCase(true, false, -4f, 0f)]
    [TestCase(false, true, 4f, 0f)]
    [TestCase(true, true, 0f, 4f)]
    public void WrongStartQuadrantDies(bool neSwReversed, bool nwSeReversed, float x, float z)
        => Flood(MainTank, o =>
            {
                o.LineNeSw = neSwReversed ? FloodDirection.Reversed : FloodDirection.Forward;
                o.LineNwSe = nwSeReversed ? FloodDirection.Reversed : FloodDirection.Forward;
            })
            .TeleportAt(6.25f, to: new(x, z))
            .ShouldKill(ActionId.FloodAOE, MainTank);

    [Test]
    public void NotRotatingAfterSecondWaveDies()
        => Flood(MainTank)
            .FreezeAt(7.52f)
            .ShouldKill(ActionId.FloodAOE, MainTank);

    // NE-SW leading rotates the party East, NW-SE leading West; the player goes the other way.
    [TestCase(true, -4f)]
    [TestCase(false, 4f)]
    public void RotatingTheWrongWayDies(bool neSwFirst, float x)
        => Flood(MainTank, o => o.NeSwFirst = neSwFirst)
            .TeleportAt(8.29f, to: new(x, 0))
            .ShouldKill(ActionId.FloodAOE, MainTank);

    // West, 9y from the stack and clear of tick 0's lanes.
    [Test]
    public void LeavingStackKillsTheRest()
        => Flood(MainTank)
            .TeleportAt(6.25f, to: new(-8f, 0f))
            .ShouldKill(ActionId.ChaoticFlood, AllBut(MainTank));

    [Test]
    public void StackTargetLeavingStackDies()
        => Flood(RegenHealer)
            .TeleportAt(6.25f, to: new(-8f, 0f))
            .ShouldKill(ActionId.ChaoticFlood, RegenHealer);

    [Test]
    public void StayingAfterThirdWaveDies()
        => Flood(MainTank)
            .FreezeAt(8.55f)
            .ShouldKill(ActionId.FloodAOE, MainTank);
}
