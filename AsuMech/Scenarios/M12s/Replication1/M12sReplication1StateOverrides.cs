namespace AsuMech.Scenarios.M12s.Replication1;

// Pins the random rolls; null keeps the roll. Indices as in M12sReplication1State.
public sealed class M12sReplication1StateOverrides
{
    public int? FireCardinal { get; set; }
    public bool? VerticalCleaves { get; set; }
    public int? OuterFireIntercard { get; set; }
    public bool? SnakingKickEast { get; set; }
    public int? SobatTurns { get; set; }
}
