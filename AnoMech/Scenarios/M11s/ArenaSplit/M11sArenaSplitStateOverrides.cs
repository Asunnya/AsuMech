namespace AnoMech.Scenarios.M11s.ArenaSplit;

// Pins the random rolls; null keeps the roll.
public sealed class M11sArenaSplitStateOverrides
{
    // 1 or 2: the round whose Meteowrath tether the player takes.
    public int? PlayerTetherRound { get; set; }
    // First round's meteor lines at x -21 and 11 (else -11 and 21).
    public bool? FirstLinesWestOuter { get; set; }
    public bool? SecondLinesWestOuter { get; set; }
    // 0 north-east, 1 south-east, 2 south-west, 3 north-west: the corner Arcadion Avalanche spares.
    public int? Avalanche { get; set; }
}
