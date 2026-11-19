using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Fru.P2MirrorMirror;

// Hallowed Ray on anyone; the blue mirror on any spoke, the two reds 90° apart on two others;
// Banish as pairs or spreads.
//
// Simplification: retail's tank drags the Usurper away from the blue mirror for the kicks; here
// she walks there herself.
public sealed class FruP2MirrorMirrorState
{
    public const float MirrorRadius = 20f;
    public const float BossDistance = 7.5f;
    private const int ClearRedSpots = 6;

    public int BlueOctant { get; }
    public int FirstRedOctant { get; }
    public PartyRole RayTarget { get; }
    public bool Pairs { get; }
    public bool SupportPairs { get; }

    public FruP2MirrorMirrorState(Rng rng, FruP2MirrorMirrorStateOverrides overrides)
    {
        var blue = rng.Next(8);
        var red = rng.Next(ClearRedSpots);
        var rayTarget = (PartyRole)rng.Next(8);
        var pairs = rng.NextBool();
        var supportPairs = rng.NextBool();
        BlueOctant = overrides.BlueOctant ?? blue;
        var reds = Enumerable.Range(0, 8).Where(RedsClearOfBlue).ToArray();
        FirstRedOctant = overrides.FirstRedOctant is { } forced && RedsClearOfBlue(forced) ? forced : reds[red];
        RayTarget = overrides.RayTarget ?? rayTarget;
        Pairs = overrides.Pairs ?? pairs;
        SupportPairs = overrides.SupportPairs ?? supportPairs;
    }

    private bool RedsClearOfBlue(int first) => first != BlueOctant && (first + 2) % 8 != BlueOctant;

    public int[] RedOctants => [FirstRedOctant, (FirstRedOctant + 2) % 8];

    public Vector3 Blue => Point(BlueOctant, MirrorRadius);
    public Vector3 BossSpot => Point(BlueOctant + 4, BossDistance);

    // Octants clockwise from north.
    public static Vector3 Point(float octant, float radius)
        => new(MathF.Sin(octant * MathF.PI / 4) * radius, 0, -MathF.Cos(octant * MathF.PI / 4) * radius);
}
