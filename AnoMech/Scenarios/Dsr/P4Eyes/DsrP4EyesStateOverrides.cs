using System.Collections.Generic;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Dsr.P4Eyes;

// Pins the random rolls; null keeps the roll.
public sealed class DsrP4EyesStateOverrides
{
    public PartyRole? SoulMarked { get; set; }
    // The four Hatebound puts on red (Clawbound); the rest get blue.
    public IReadOnlyCollection<PartyRole>? FirstRed { get; set; }
    // Per Mirage Dive, the two red holders hit, as indices into the red holders in party order.
    public IReadOnlyList<(int, int)>? DivePicks { get; set; }
}
