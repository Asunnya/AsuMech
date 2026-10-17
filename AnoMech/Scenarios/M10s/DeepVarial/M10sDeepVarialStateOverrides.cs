namespace AnoMech.Scenarios.M10s.DeepVarial;

// Pins the random rolls; null keeps the roll.
public sealed class M10sDeepVarialStateOverrides
{
    // The player's element: true fire, false water.
    public bool? PlayerFire { get; set; }
    public bool? WaveFromNorth { get; set; }
    // The small wave's orbs before Deep Varial: true four (spreads), false one (a stack).
    public bool? VarialSpread { get; set; }
    // Deep Blue's lane: -1 west, 0 middle, 1 east.
    public int? Lane { get; set; }
    // Deep Blue's orbs before Sickest Take-off: true spreads, false a stack.
    public bool? TakeoffSpread { get; set; }
}
