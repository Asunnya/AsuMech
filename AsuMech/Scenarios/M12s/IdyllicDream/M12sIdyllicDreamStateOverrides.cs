namespace AsuMech.Scenarios.M12s.IdyllicDream;

// Pins the random rolls; null keeps the roll.
public sealed class M12sIdyllicDreamStateOverrides
{
    // 0 north, clockwise to 7 north-west: where the player's own clone stands.
    public int? PlayerCloneDirection { get; set; }
    public bool? CardinalClonesFirst { get; set; }
    public bool? CardinalBossClonesDefa { get; set; }
    public bool? SpreadOnSupports { get; set; }
    // -1 west, 1 east: the platform the Snaking Kick clone jumps to.
    public int? KickPlatform { get; set; }
    public bool? TearTakesEastWestClone { get; set; }
}
