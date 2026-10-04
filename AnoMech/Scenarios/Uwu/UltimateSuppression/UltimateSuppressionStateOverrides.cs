using System.Collections.Generic;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Uwu.UltimateSuppression;

public enum UltimateSuppressionAssignment
{
    Auto,
    LightPillar,
    MistralSong,
    Eruption,
    Gaol
}

public class UltimateSuppressionStateOverrides
{
    public UltimateSuppressionAssignment Assignment { get; set; }

    // Light Pillar, both Mistral Songs, both Eruptions, Gaol; wins over Assignment.
    internal IReadOnlyList<PartyRole>? Assignments { get; set; }
    internal PartyRole? FlamingCrush { get; set; }
    internal PartyRole? ThermalLowHealer { get; set; }
    internal int[]? SuppressionSpotOrder { get; set; }

    // One Aetherochemical Laser action per cast, in cast order.
    internal IReadOnlyList<uint>? Lasers { get; set; }

    // Every Feather Rain drops on these, instead of five random members each time.
    internal IReadOnlyList<PartyRole>? FeatherRainTargets { get; set; }

    internal PartyRole? GarudaFacing { get; set; }
    internal PartyRole? LandslideBait { get; set; }
}
