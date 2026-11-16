namespace AsuMech.Scenarios.M12s.Slaughtershed;

// Pins the random rolls; null keeps the roll.
public sealed class M12sSlaughtershedStateOverrides
{
    // Mortal Slayer's purple orbs, and so the supports, on the east.
    public bool? PurpleEast { get; set; }
    // True: the player's role takes the spreads every Slaughtershed; false: the stack.
    public bool? PlayerSpreads { get; set; }
    // 0-2: the Slaughtershed whose arms are the dragon heads' cleaves; the others knock back.
    public int? SerpentineRound { get; set; }
}
