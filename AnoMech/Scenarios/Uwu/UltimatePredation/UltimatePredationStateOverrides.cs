using System.Collections.Generic;
using AnoMech.Core.Game.Party;
using static AnoMech.Scenarios.Uwu.UwuConstants;

namespace AnoMech.Scenarios.Uwu.UltimatePredation;

public class UltimatePredationStateOverrides
{
    /// <value>
    /// If <see langword="null"/>, the bosses will be positioned randomly.
    /// If not <see langword="null"/> and <see langword="true"/>, the bosses will be positioned semi-randomly, allowing to execute the center dodge.
    /// </value>
    public bool? CenterDodge { get; set; }

    // Keys into Geometry's <Boss>Placements. A pin wins over CenterDodge's restrictions.
    internal DirectionEnum? Garuda { get; set; }
    internal DirectionEnum? Ultima { get; set; }
    internal DirectionEnum? Ifrit { get; set; }
    internal DirectionEnum? Titan { get; set; }
    internal bool? TitanOffsetRight { get; set; }

    // Index into Geometry.TitanBoulderPositions of the first bomb; the rest follow in order.
    internal int? BoulderStart { get; set; }

    // Every Feather Rain drops on these, instead of five random members each time.
    internal IReadOnlyList<PartyRole>? FeatherRainTargets { get; set; }

    internal PartyRole? UltimaLandslideBait { get; set; }
    internal PartyRole? TitanLandslideBait { get; set; }
    internal PartyRole? InfernalFettersDps { get; set; }
}
