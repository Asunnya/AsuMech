using System.Collections.Generic;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Ucob.P3Tenstrike;

// Pins the random rolls; null keeps the roll.
public sealed class UcobP3TenstrikeStateOverrides
{
    // A role that must be among Twintania's Hatch targets.
    public PartyRole? HatchInclude { get; set; }
    // Roles pinned to the first or the second Earth Shaker set.
    public PartyRole? FirstEarthShakerInclude { get; set; }
    public PartyRole? SecondEarthShakerInclude { get; set; }

    public IReadOnlyList<PartyRole>? HatchTargets { get; set; }
    public IReadOnlyList<PartyRole>? MeteorOrder { get; set; }
    public IReadOnlyList<PartyRole>? FirstEarthShakers { get; set; }
}
