namespace AnoMech.Scenarios.M10s.XtremeSnaking;

// Pins the random rolls; null keeps the roll.
public sealed class M10sXtremeSnakingStateOverrides
{
    // The player's element: true fire, false water. Picks whether the snakes take the nearest or the furthest.
    public bool? PlayerFire { get; set; }
    public bool? FireOnTheNearest { get; set; }
    public int? Pattern { get; set; }
    public bool? RedHotEndsInTheMiddle { get; set; }
}
