namespace AnoMech.Scenarios.M10s.Snaking;

// Pins the random rolls; null keeps the roll.
public sealed class M10sSnakingStateOverrides
{
    // The player's element: true fire, false water. Picks whether the snakes take the nearest or the furthest.
    public bool? PlayerFire { get; set; }
    public bool? FireOnTheNearest { get; set; }
    public bool? DeepBlueSouth { get; set; }
    public bool? DoubleDip { get; set; }
    public bool? WaveFromNorth { get; set; }
}
