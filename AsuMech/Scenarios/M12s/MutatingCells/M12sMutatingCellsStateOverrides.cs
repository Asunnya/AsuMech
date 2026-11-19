namespace AsuMech.Scenarios.M12s.MutatingCells;

// Pins the random rolls; null keeps the roll.
public sealed class M12sMutatingCellsStateOverrides
{
    // The player's first mutation: true α, false β.
    public bool? PlayerAlphaFirst { get; set; }
    // -1 west, 1 east: the black hole with two orbs close to it.
    public int? CloseSide { get; set; }
    // The two close orbs: true water and thunder, false aero and fire.
    public bool? CloseWaterThunder { get; set; }
    public bool? NetherworldFar { get; set; }
    // 0..3: Double Sobat's second kick turned 0, 90 (left), 180 or -90 (right) degrees from the first.
    public int? SobatTurn { get; set; }
}
