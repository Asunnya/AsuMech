using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Uwu.P1Garuda;

// Rolls the player can pin; null keeps the seed's own. The rng is drawn either way, so pinning one
// roll leaves every other roll of the seed as it was.
public class UwuP1GarudaStateOverrides
{
    // Compass degrees, a cardinal each.
    public float? SuparnaSongBearing { get; set; }
    public float? ChiradaSongBearing { get; set; }
    public PartyRole? MistralSongTarget { get; set; }
    public List<PartyRole>? SistersSongTargets { get; set; }
    public List<PartyRole>? FrictionTargets { get; set; }
    // Four of the twelve spawn spots each, as (x,z).
    public List<Vector2>? SatinPlumesFirst { get; set; }
    public List<Vector2>? SatinPlumesSecond { get; set; }
    public List<PartyRole>? MesohighTargets { get; set; }

    public void Clear()
    {
        SuparnaSongBearing = ChiradaSongBearing = null;
        MistralSongTarget = null;
        SistersSongTargets = FrictionTargets = MesohighTargets = null;
        SatinPlumesFirst = SatinPlumesSecond = null;
    }

    public bool Any => SuparnaSongBearing != null || ChiradaSongBearing != null || MistralSongTarget != null || SistersSongTargets != null
                       || FrictionTargets != null || SatinPlumesFirst != null || SatinPlumesSecond != null || MesohighTargets != null;
}
