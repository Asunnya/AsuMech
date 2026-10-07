using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Umad.P4KefkaSays;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Tests.NegativeRun;
using ActionId = AnoMech.Scenarios.Umad.UmadConstants.ActionId;

namespace AnoMech.Tests;

// Every roll is pinned. Each Mystery's real Blizzard cones face SE and NW, and its real Thunder
// lines run SW over the bands -14.2 < x+z < 0 and 14.2 < x+z < 28.4; Blizzard tells the truth on
// the even casts and Thunder on the odd ones. Wave1 = MT OT RH SH M1 M2 R C, Wave2 = RH SH MT OT
// R C M1 M2, Wave3 = role order, all wounds Black. Exdeath 1 real, 2 lying, 3 and 4 real; Inferno
// first, both Chaos casts real; Neo Exdeath north, White Antilight west.
//
//   15.72 30.64 45.78 115.33  Mystery cones and lines; 79.15 lines only, 97.11 cones only.
//   62.90  Edge of Death down x=0. Beyond Death on MT RH M1 R, east at (7.6,1) under the Black
//          Antilight; Allagan Field on OT SH M2 C, west at (-8,0) under the White. 66.34 an
//          uncleansed Beyond Death kills.
//   71.28  C's Acceleration Bomb (lying); 71.36 MT's (real).
//   71.38  Wave 1 (true): Death Wave on SH stacked N (0,-12) with MT OT, Death Bolt on RH alone
//          W (-12,0); Wave on C stacked S (0,12) with M1 M2, Bolt on R alone E (12,0).
//   80.49  Death Shriek (true, look away) from MT (2.9,-1.1) and M1 (-1.1,2.9); the rest line up
//          on that diagonal, C furthest SW at (-6.5,8.3).
//   87.28  Stray Flames baits lock on the party stacked in the middle; the chariots land 92.06
//          with everyone out at r=11, MT at (0,11).
//   96.49  Wave 2 (lying): Death Bolt on MT stacked N (0.8,-12) with SH RH, Death Wave on OT
//          alone W (-12.2,1.1); Bolt on M1 stacked S with R C, Wave on M2 alone E (12.1,-1.2).
//  104.39  Death Shriek (lying, look at) from RH (0,-1.4) and R (0,1.4); C at (1.2,6.9).
//  109.98  Stray Spray baits lock in the middle; the donuts land 114.77 with the party at (4.1,-1.6).
//
// A run pauses 5s after its first death.
public class UmadP4KefkaSaysScenarioTests
{
    private const float FacingNorth = MathF.PI;
    private const float FacingSouth = 0f;
    private const float FacingNorthEast = 3f * MathF.PI / 4f;
    private const float FacingSouthWest = -MathF.PI / 4f;

    internal static void Pin(UmadP4KefkaSaysStateOverrides o)
    {
        for (var k = 0; k < 5; k++)
        {
            o.BlizzardReal[k] = k % 2 == 0;
            o.LightningReal[k] = k % 2 == 1;
            o.BlizzardOffset[k] = 0;
            o.LightningOffset[k] = 0;
            o.LightningOrientation[k] = 1f;
        }
        o.ExdeathCast1Real = true;
        o.ExdeathCast2 = ExdeathCast2Mode.Fake;
        o.ExdeathCast3Real = true;
        o.ExdeathCast4Real = true;
        o.ChaosCast1Real = true;
        o.ChaosCast2Real = true;
        o.InfernoFirst = true;
        o.Wave1First = true;
        o.Wave1 = [MainTank, OffTank, RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
        for (var i = 0; i < 4; i++) o.Wave2Swaps[i] = false;
        o.Wave3 = [MainTank, OffTank, RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
        o.Wounds = new bool[8];
        o.NeoExdeathDirection = Direction.N;
        o.Antilight0White = true;
    }

    private static NegativeRun<UmadP4KefkaSaysScenario> KefkaSays(PartyRole player, Action<UmadP4KefkaSaysStateOverrides>? tweak = null)
        => Negative<UmadP4KefkaSaysScenario>(player)
            .Overrides<UmadP4KefkaSaysStateOverrides>(o =>
            {
                Pin(o);
                tweak?.Invoke(o);
            });

    // (4,4) is in the SE cone and between the Thunder bands.
    [TestCase(15.72f, ActionId.BlizzardIIIBlowout_Real)]
    [TestCase(30.64f, ActionId.BlizzardIIIBlowout_FakeAnim)]
    [TestCase(45.78f, ActionId.BlizzardIIIBlowout_Real)]
    [TestCase(97.11f, ActionId.BlizzardIIIBlowout_FakeAnim)]
    [TestCase(115.33f, ActionId.BlizzardIIIBlowout_Real)]
    public void StandingInBlizzardConeDies(float hit, uint cone)
        => KefkaSays(CasterDps)
            .TeleportAt(hit - 0.3f, to: new(4, 4))
            .ShouldKill(cone, CasterDps);

    // (3,-6) is in a Thunder band and between the cones.
    [TestCase(15.72f, ActionId.ThrummingThunderIII_FakeAnim)]
    [TestCase(30.64f, ActionId.ThrummingThunderIII_Real)]
    [TestCase(45.78f, ActionId.ThrummingThunderIII_FakeAnim)]
    [TestCase(79.15f, ActionId.ThrummingThunderIII_Real)]
    [TestCase(115.33f, ActionId.ThrummingThunderIII_FakeAnim)]
    public void StandingInThunderLineDies(float hit, uint line)
        => KefkaSays(CasterDps)
            .TeleportAt(hit - 0.3f, to: new(3, -6))
            .ShouldKill(line, CasterDps);

    [Test]
    public void StandingOnEdgeOfDeathDies()
        => KefkaSays(MainTank)
            .TeleportAt(62.4f, to: new(0, 1))
            .ShouldKill(ActionId.EdgeOfDeath, MainTank);

    // MT holds Beyond Death, OT Allagan Field. A lying Exdeath 3 swaps both the debuff and the Wound
    // colour each one reads as. Exdeath 4's truth only changes the Flood of Naught tell and never
    // which side kills. White is west (x=-8), Black east (x=8).
    [TestCase(true, true, MainTank, false, -8f)]
    [TestCase(true, false, MainTank, false, -8f)]
    [TestCase(true, true, MainTank, true, 8f)]
    [TestCase(true, false, MainTank, true, 8f)]
    [TestCase(false, true, OffTank, false, 8f)]
    [TestCase(false, false, OffTank, false, 8f)]
    [TestCase(false, true, OffTank, true, -8f)]
    [TestCase(false, false, OffTank, true, -8f)]
    public void BeyondDeathOutsideItsWoundColourDies(bool debuffsReal, bool floodReal, PartyRole holder, bool whiteWound, float x)
        => KefkaSays(holder, o => Antilights(o, debuffsReal, floodReal, holder, whiteWound))
            .TeleportAt(62.4f, to: new(x, 1))
            .ShouldKill(TheEnvironment, holder);

    [TestCase(true, true, OffTank, false, 8f, ActionId.BlackAntilight)]
    [TestCase(true, false, OffTank, false, 8f, ActionId.BlackAntilight)]
    [TestCase(true, true, OffTank, true, -8f, ActionId.WhiteAntilight)]
    [TestCase(true, false, OffTank, true, -8f, ActionId.WhiteAntilight)]
    [TestCase(false, true, MainTank, false, -8f, ActionId.WhiteAntilight)]
    [TestCase(false, false, MainTank, false, -8f, ActionId.WhiteAntilight)]
    [TestCase(false, true, MainTank, true, 8f, ActionId.BlackAntilight)]
    [TestCase(false, false, MainTank, true, 8f, ActionId.BlackAntilight)]
    public void AllaganFieldInItsWoundColourWipes(bool debuffsReal, bool floodReal, PartyRole holder, bool whiteWound, float x, uint antilight)
        => KefkaSays(holder, o => Antilights(o, debuffsReal, floodReal, holder, whiteWound))
            .TeleportAt(62.4f, to: new(x, 1))
            .ShouldKill(antilight, holder)
            .ShouldKill(ActionId.DeathSurge, AllBut(holder));

    private static void Antilights(UmadP4KefkaSaysStateOverrides o, bool debuffsReal, bool floodReal, PartyRole holder, bool whiteWound)
    {
        o.ExdeathCast3Real = debuffsReal;
        o.ExdeathCast4Real = floodReal;
        o.Wounds![Array.IndexOf(o.Wave3!, holder)] = whiteWound;
    }

    [TestCase(70.9f, OffTank, ActionId.DeathWave, MainTank, ShieldHealer)]
    [TestCase(96.0f, ShieldHealer, ActionId.DeathBolt, MainTank, RegenHealer)]
    public void LeavingElementStackKillsStackers(float at, PartyRole leaver, uint stack, params PartyRole[] stackers)
        => KefkaSays(leaver)
            .TeleportAt(at, to: new(0, 0))
            .ShouldKill(stack, stackers);

    // The solo's own hit applies the vuln; the stack's hit, resolving after it, kills.
    [TestCase(70.9f, RegenHealer, 0f, -12f)]
    [TestCase(96.0f, OffTank, 0.8f, -12f)]
    public void SoloMarkerJoiningStackKillsStack(float at, PartyRole solo, float x, float z)
        => KefkaSays(solo)
            .TeleportAt(at, to: new(x, z))
            .ShouldKill(ActionId.DeathWave, MainTank, OffTank, RegenHealer, ShieldHealer);

    [TestCase(70.9f, RegenHealer, 12f, 0f, ActionId.DeathBolt, PhysRangedDps)]
    [TestCase(96.0f, OffTank, 12.1f, -1.2f, ActionId.DeathWave, MeleeDpsB)]
    public void TwoSoloMarkersTogetherKillBoth(float at, PartyRole solo, float x, float z, uint marker, PartyRole otherSolo)
        => KefkaSays(solo)
            .TeleportAt(at, to: new(x, z))
            .ShouldKill(marker, solo, otherSolo);

    // Wave 2 lies, so MT's Bolt is the one that needs three. A bot carries the stack marker: a
    // player SH standing on the solo spot dies first to their own lying Acceleration Bomb at 71.28.
    [TestCase(70.9f, ShieldHealer, -12f, 0f, ActionId.DeathWave, RegenHealer)]
    [TestCase(96.0f, MainTank, -12.2f, 1.1f, ActionId.DeathBolt, OffTank)]
    public void StackMarkerOnSoloSpotKillsBoth(float at, PartyRole stackMarker, float x, float z, uint marker, PartyRole solo)
        => KefkaSays(solo)
            .MoveBotAt(at, stackMarker, to: new(x, z))
            .ShouldKill(marker, stackMarker, solo);

    // C stands on the gaze diagonal, south-west of both sources.
    [TestCase(true, FacingNorthEast)]
    [TestCase(false, FacingSouthWest)]
    public void FirstDeathShriekFacingWrongWayDies(bool real, float facing)
        => KefkaSays(CasterDps, o => o.ExdeathCast1Real = real)
            .TeleportAt(80.2f, to: new(-6.5f, 8.3f), facing: facing)
            .ShouldKill(ActionId.DeathShriek, CasterDps);

    // C stands south of both sources.
    [TestCase(true, FacingNorth)]
    [TestCase(false, FacingSouth)]
    public void SecondDeathShriekFacingWrongWayDies(bool real, float facing)
        => KefkaSays(CasterDps, o => o.ExdeathCast2 = real ? ExdeathCast2Mode.Real : ExdeathCast2Mode.Fake)
            .TeleportAt(104.1f, to: new(1.2f, 6.9f), facing: facing)
            .ShouldKill(ActionId.DeathShriek, CasterDps);

    [TestCase(true, 0f, 0f, ActionId.StrayFlames_Chariot)]
    [TestCase(false, 0f, 10f, ActionId.StrayFlames_Donut)]
    public void IgnoringStrayFlamesShapeDies(bool real, float x, float z, uint shape)
        => KefkaSays(CasterDps, o => o.ChaosCast1Real = real)
            .TeleportAt(91.8f, to: new(x, z))
            .ShouldKill(shape, CasterDps);

    // (-3,10) clears the last Mystery's cones and lines, so only the donut can kill there.
    [TestCase(true, -3f, 10f, ActionId.StraySpray_Donut)]
    [TestCase(false, 0f, 0f, ActionId.StraySpray_Chariot)]
    public void IgnoringStraySprayShapeDies(bool real, float x, float z, uint shape)
        => KefkaSays(CasterDps, o => o.ChaosCast2Real = real)
            .TeleportAt(114.5f, to: new(x, z))
            .ShouldKill(shape, CasterDps);

    [Test]
    public void BaitingStrayFlamesOnARunOutSpotKillsTheRunner()
        => KefkaSays(CasterDps)
            .TeleportAt(87.0f, to: new(0, 11))
            .ShouldKill(ActionId.StrayFlames_Chariot, CasterDps, MainTank);

    [Test]
    public void MovingThroughRealAccelerationBombDies()
        => KefkaSays(MainTank)
            .FreezeAt(70.9f)
            .At(71.1f, p => p.HoldMovementInput())
            .ShouldKill(ActionId.DeathBomb, MainTank);

    [Test]
    public void StandingStillThroughLyingAccelerationBombDies()
        => KefkaSays(MainTank, o => o.ExdeathCast1Real = false)
            .FreezeAt(70.9f)
            .ShouldKill(ActionId.DeathBomb, MainTank);

    [Test]
    public void DiesWalkingOffArena()
        => KefkaSays(MainTank)
            .TeleportAt(3f, to: new(0, 20.5f))
            .ShouldKill(TheEnvironment, MainTank);
}
