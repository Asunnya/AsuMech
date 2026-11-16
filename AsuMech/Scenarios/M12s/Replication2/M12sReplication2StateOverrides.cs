namespace AsuMech.Scenarios.M12s.Replication2;

// Pins the random rolls; null keeps the roll.
public sealed class M12sReplication2StateOverrides
{
    // The player clone tethered to the player, clockwise from north (0 north .. 7 northwest).
    public int? PlayerSpot { get; set; }
    public bool? NetherwrathFar { get; set; }
}
