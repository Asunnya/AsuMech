namespace AsuMech.Scenarios.M12s.Act2;

// Pins the random rolls; null keeps the roll.
public sealed class M12sAct2StateOverrides
{
    // 1-4: when the player's chain goes out.
    public int? PlayerOrder { get; set; }
    // 0 north-east, 1 south-east, 2 south-west, 3 north-west: the first blob's corner.
    public int? FirstBlob { get; set; }
    // 0 north, 1 east, 2 south, 3 west: where the boss faces on its first spin.
    public int? FirstFacing { get; set; }
}
