namespace AnoMech.Scenarios.M11s.Ecliptic;

// Pins the random rolls; null keeps the roll.
public sealed class M11sEclipticStateOverrides
{
    // Mammoth Meteors north-east and south-west (else north-west and south-east).
    public bool? MammothNortheast { get; set; }
    // Whether the player is among the first picked for Atomic Impact when standing out.
    public bool? PlayerMarked { get; set; }
}
