using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Scenarios.Ucob.P3Tenstrike;

public sealed class UcobP3TenstrikeTuuflessAi : UcobP3TenstrikeAi, IUcobStrat
{
    public override string Name => "Tuufless (Elemental)";
    public UcobMarkers Markers => UcobMarkers.Elemental;

    private static readonly PartyRole[] LineupWestToEast = [PhysRangedDps, RegenHealer, MeleeDpsA, MainTank, OffTank, MeleeDpsB, ShieldHealer, CasterDps];
    private const float LineupSpacing = 3.25f;
    private const float ClearOfTheSecondHatch = 10f;
    private static readonly IReadOnlyList<Vector2> NeurolinksWestToEast = UcobMarkers.Elemental.Place(Geometry.Neurolinks).OrderBy(n => n.X).ToList();
    private static readonly Vector2 SouthNeurolink = NeurolinksWestToEast.MaxBy(n => n.Y);
    private static readonly float[] EdgesWestRoundTheSouthToEast = [265f, 225f, 150f, 115f, 95f];
    private static readonly int[] EdgeCuttingIntoEachNeurolinkWestToEast = [0, 1, 4];
    private static readonly float[] AlongTheNorthWestToEast = [255f, 325f, 395f, 465f];

    protected override IReadOnlyList<Vector2> Neurolinks => NeurolinksWestToEast;
    protected override float[] EdgeBearings => EdgesWestRoundTheSouthToEast;
    protected override float EdgeRadius => 18.5f;
    protected override int[] EdgeCuttingIntoNeurolink => EdgeCuttingIntoEachNeurolinkWestToEast;
    protected override float[] ShakerBearings => AlongTheNorthWestToEast;
    protected override float ShakerArcStart => 240f;
    protected override float ShakerArcEnd => 480f;
    protected override float FirstShakerRadius => 18f;
    protected override float SecondShakerRadius => 11.5f;
    protected override Vector2 WaitingSpot => SouthNeurolink;

    protected override void AssignHatchesAndEdges()
    {
        hatchByNeurolink = InLineupOrder(state.HatchTargets);
        waitingAtEdge = InLineupOrder(Enum.GetValues<PartyRole>().Except(state.HatchTargets));
    }

    protected override Vector2?[] HomeSpots()
    {
        var spots = new Vector2?[8];
        for (var i = 0; i < LineupWestToEast.Length; i++)
            spots[(int)LineupWestToEast[i]] = new Vector2((i - (LineupWestToEast.Length - 1) / 2f) * LineupSpacing, 0f);
        return spots;
    }

    protected override Vector2 OutBehind(Vector2 neurolink) => Vector2.Normalize(neurolink) * (neurolink.Length() + ClearOfTheSecondHatch);

    protected override float BearingToAssignFrom(PartyRole role)
    {
        var hatch = IndexIn(hatchByNeurolink, role);
        if (hatch >= 0) return BearingOf(Neurolinks[hatch]);
        var edge = IndexIn(waitingAtEdge, role);
        var cut = Array.IndexOf(EdgeCuttingIntoNeurolink, edge);
        return cut >= 0 ? BearingOf(Neurolinks[cut]) : EdgeBearings[edge];
    }

    private static IReadOnlyList<PartyRole> InLineupOrder(IEnumerable<PartyRole> roles)
        => roles.OrderBy(r => Array.IndexOf(LineupWestToEast, r)).ToList();

    private static int IndexIn(IReadOnlyList<PartyRole> roles, PartyRole role)
    {
        for (var i = 0; i < roles.Count; i++)
            if (roles[i] == role) return i;
        return -1;
    }
}
