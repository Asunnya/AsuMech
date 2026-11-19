namespace AsuMech.Scenarios.M12s.Act3;

// Pins the random rolls; null keeps the roll.
public sealed class M12sAct3StateOverrides
{
    // 0 north, 1 east, 2 south, 3 west: the way the player's growth points.
    public int? PlayerGrowth { get; set; }
    // True: the cardinal platforms stay; false: the intercardinal ones.
    public bool? CardinalPlatforms { get; set; }
}
