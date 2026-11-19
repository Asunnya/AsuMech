using System.Numerics;
using AsuMech.Scenarios.Ucob.P4Adds;

namespace AsuMech.Scenarios.Ucob.P5GoldenBahamut;

public sealed class UcobP5GoldenBahamutTuuflessAi : UcobP5GoldenBahamutAi, IUcobStrat
{
    public override string Name => "Tuufless (Elemental)";
    public UcobMarkers Markers => UcobMarkers.Elemental;

    public static readonly Vector2 FirstAkhMornOnTheBottomNeurolink = UcobP4AddsTuuflessAi.BottomNeurolink;
    public static readonly Vector2 PartyNorthOfTheBoss = new(0f, -4f);
    public static readonly Vector2 TanksTowardTheCenter = new(0f, 4f);
    public static readonly Vector2 PartyAtTheNorthEdge = new(0f, -12f);

    protected override Vector2 AkhMornTankSpot(int akhMorn) => akhMorn == 0 ? FirstAkhMornOnTheBottomNeurolink : TanksTowardTheCenter;

    protected override Vector2 AkhMornPartySpot(int akhMorn) => akhMorn == 0 ? PartyNorthOfTheBoss : PartyAtTheNorthEdge;
}
