namespace AsuMech.Scenarios.M12s.CurtainCall;

// Pins the random rolls; null keeps the roll.
public sealed class M12sCurtainCallStateOverrides
{
    // True: the player's role gets Rotting Flesh (stand in the cleave); false: Bursting Grotesquerie.
    public bool? PlayerRotting { get; set; }
    public bool? DragonEast { get; set; }
    // True: the blood vessels spare the north-west and south-east corners; false: north-east and south-west.
    public bool? NorthWestSafe { get; set; }
}
