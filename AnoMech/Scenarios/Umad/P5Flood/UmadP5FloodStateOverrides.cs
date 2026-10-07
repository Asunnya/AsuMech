using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Umad.P5Flood;

// Two crossing diagonal lines (NE-SW and NW-SE) each march across the arena through 4 points
// over the same 4 ticks; a point from each resolves together every tick. Forward = NE->SW for
// the NE-SW line, NW->SE for the NW-SE line (see UmadP5FloodScenario.ArmPoints); Reversed starts
// from the opposite end.
public enum FloodDirection
{
    Random,
    Forward,
    Reversed,
}

// Random leaves a field randomized at scenario start.
public sealed class UmadP5FloodStateOverrides
{
    public FloodDirection LineNeSw { get; set; } = FloodDirection.Random;
    public FloodDirection LineNwSe { get; set; } = FloodDirection.Random;

    // Which diagonal telegraphs first (tick0/tick2 vs tick1/tick3).
    public bool? NeSwFirst { get; set; } = null; // null = random

    // The stack target for every tick; the real fight rolls a fresh non-tank per tick.
    public PartyRole? AnchorRole { get; set; } = null; // null = random per tick

    // The party's starting cardinal quadrant (0=N,1=E,2=S,3=W) and rotation direction; derived
    // from the line rolls when null.
    public int? StartQuadrant { get; set; } = null;
    public bool? RotationClockwise { get; set; } = null;
}
