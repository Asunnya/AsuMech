namespace AsuMech.Scenarios.M12s.ReplicationOne;

// Pins the random rolls; null keeps the roll. Indices as in M12sReplicationOneState.
public sealed class M12sReplicationOneStateOverrides
{
    public int? FireCardinal { get; set; }
    public bool? VerticalCleaves { get; set; }
    public int? OuterFireIntercard { get; set; }
    public bool? SnakingKickEast { get; set; }
    public int? SobatTurns { get; set; }
}
