using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P5Celestriad;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Tests.NegativeRun;
using ActionId = AnoMech.Scenarios.Umad.UmadConstants.ActionId;

namespace AnoMech.Tests;

// Every roll is pinned. Sectors clockwise from NE are Fire (towers at 20/60/100 degrees), Lightning
// (140/180/220), Ice (260/300/340), all at r=10. Fire, Ice, then Lightning doubles; a single element
// lights its middle tower, the doubled one its outer two, the first for its debuffed pair and the
// second for the free pair. Debuffs: MT SH Fire, OT M1 Ice, RH M2 Lightning, R C free. Set 1's
// Catastrophic Choice is Earth (stand on the tower's outer half), set 3's Aero (inner half).
//
//   14.18  Set 1: OT M1 Fire (3.4,-9.4), R C Fire (9.8,1.7), MT SH Lightning (0,10),
//          RH M2 Ice (-8.7,-5.0). Earth from Kefka, 10y circle.
//   20.50  Set 2: MT SH Ice (-9.8,1.7), R C Ice (-3.4,-9.4), RH M2 Fire (8.7,-5.0),
//          OT M1 Lightning (0,10).
//   26.34  Set 3: RH M2 Lightning (6.4,7.7), R C Lightning (-6.4,7.7), MT SH Fire (8.7,-5.0),
//          OT M1 Ice (-8.7,-5.0). Aero from Kefka, donut outside 10y. The starting debuffs ran
//          out at 26.1.
//
// A run pauses 5s after its first death.
public class UmadP5CelestriadScenarioTests
{
    internal static void Pin(UmadP5CelestriadStateOverrides o)
    {
        o.SectorOrder = CelestriadElementOrder.FireLightningIce;
        o.DoubleOrder = CelestriadElementOrder.FireIceLightning;
        o.FixedLitTowers = true;
        o.Set1 = CatastrophicVariantOverride.Earth;
        o.Set3 = CatastrophicVariantOverride.Aero;
        o.Debuff[MainTank] = CelestriadDebuff.Fire;
        o.Debuff[ShieldHealer] = CelestriadDebuff.Fire;
        o.Debuff[OffTank] = CelestriadDebuff.Ice;
        o.Debuff[MeleeDpsA] = CelestriadDebuff.Ice;
        o.Debuff[RegenHealer] = CelestriadDebuff.Lightning;
        o.Debuff[MeleeDpsB] = CelestriadDebuff.Lightning;
        o.Debuff[PhysRangedDps] = CelestriadDebuff.Free;
        o.Debuff[CasterDps] = CelestriadDebuff.Free;
    }

    private static NegativeRun<UmadP5CelestriadScenario> Celestriad(PartyRole player, Action<UmadP5CelestriadStateOverrides>? tweak = null)
        => Negative<UmadP5CelestriadScenario>(player)
            .Overrides<UmadP5CelestriadStateOverrides>(o =>
            {
                Pin(o);
                tweak?.Invoke(o);
            });

    // MT joins OT M1's Fire tower in set 1 (outer half, clear of Earth) and RH M2's in set 2.
    [TestCase(14f, 3.9f, -10.8f)]
    [TestCase(20.3f, 8.7f, -5f)]
    public void SoakingOwnDebuffElementDies(float at, float x, float z)
        => Celestriad(MainTank)
            .TeleportAt(at, to: new(x, z))
            .ShouldKill(ActionId.CelestriadFireIII, MainTank);

    // Set 2: MT takes Lightning again, C Fire. Set 3: both take Ice again, on Aero's inner half.
    [TestCase(MainTank, 20.3f, 0f, 10f, ActionId.CelestriadThunderIII)]
    [TestCase(CasterDps, 20.3f, 8.7f, -5f, ActionId.CelestriadFireIII)]
    [TestCase(MainTank, 26.2f, -7.4f, -4.3f, ActionId.CelestriadBlizzardIII)]
    [TestCase(CasterDps, 26.2f, -7.4f, -4.3f, ActionId.CelestriadBlizzardIII)]
    public void RepeatingLastSetsElementDies(PartyRole player, float at, float x, float z, uint tower)
        => Celestriad(player)
            .TeleportAt(at, to: new(x, z))
            .ShouldKill(tower, player);

    // MT leaves SH alone, standing between two towers on Earth's safe side in set 1 and at
    // Kefka otherwise.
    [TestCase(14f, -4.1f, 11.3f, ActionId.CelestriadThunderIII)]
    [TestCase(20.3f, 0f, 0f, ActionId.CelestriadBlizzardIII)]
    [TestCase(26.2f, 0f, 0f, ActionId.CelestriadFireIII)]
    public void LeavingYourTowerKillsPartner(float at, float x, float z, uint tower)
        => Celestriad(MainTank)
            .TeleportAt(at, to: new(x, z))
            .ShouldKill(tower, ShieldHealer);

    // MT stays in its tower, on the half the variant hits.
    [TestCase(14f, CatastrophicVariantOverride.Earth, 0f, 8.5f)]
    [TestCase(14f, CatastrophicVariantOverride.Aero, 0f, 11.5f)]
    [TestCase(26.2f, CatastrophicVariantOverride.Earth, 7.4f, -4.3f)]
    [TestCase(26.2f, CatastrophicVariantOverride.Aero, 10f, -5.8f)]
    public void WrongHalfForCatastrophicChoiceDies(float at, CatastrophicVariantOverride variant, float x, float z)
        => Celestriad(MainTank, o =>
            {
                o.Set1 = variant;
                o.Set3 = variant;
            })
            .TeleportAt(at, to: new(x, z))
            .ShouldKill(variant == CatastrophicVariantOverride.Aero
                ? ActionId.CatastrophicChoiceAero_Resolve
                : ActionId.CatastrophicChoiceEarth_Resolve, MainTank);

    [Test]
    public void DiesWalkingOffArena()
        => Celestriad(MainTank)
            .TeleportAt(3f, to: new(0, 20.5f))
            .ShouldKill(TheEnvironment, MainTank);
}
