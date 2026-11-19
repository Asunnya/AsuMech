using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Fru.P2MirrorMirror;

// User-forced rolls for FruP2MirrorMirrorState; null leaves a field randomized.
public sealed class FruP2MirrorMirrorStateOverrides
{
    // Octants clockwise from north.
    public int? BlueOctant { get; set; }
    // The second red sits two octants clockwise of it; ignored when either would take the blue's spot.
    public int? FirstRedOctant { get; set; }
    public PartyRole? RayTarget { get; set; }
    public bool? Pairs { get; set; }
    public bool? SupportPairs { get; set; }
}
