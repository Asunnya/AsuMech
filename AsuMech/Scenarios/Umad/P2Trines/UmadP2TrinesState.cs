using System.Collections.Generic;
using System.Linq;

namespace AsuMech.Scenarios.Umad.P2Trines;

// Seven trines sit at fixed spots; three land first, then one, then the middle one with the last two.
public sealed class UmadP2TrinesState
{
    public const int Middle = 0;

    public bool CleaveWest { get; }
    public IReadOnlyList<int> FirstSet { get; }
    public int SecondSet { get; }
    public IReadOnlyList<int> ThirdSet { get; }

    public UmadP2TrinesState(Rng rng, UmadP2TrinesStateOverrides? overrides = null)
    {
        overrides ??= new UmadP2TrinesStateOverrides();
        var cleaveWest = rng.NextBool();
        var outer = rng.Shuffle(1, 2, 3, 4, 5, 6).ToList();
        CleaveWest = overrides.CleaveWest ?? cleaveWest;
        if (overrides.FirstSet is { Length: 3 } pinned && pinned.All(i => i is >= 1 and <= 6) && pinned.Distinct().Count() == 3)
            outer = [.. pinned, .. outer.Except(pinned)];
        FirstSet = outer.Take(3).ToList();
        SecondSet = outer[3];
        ThirdSet = [Middle, outer[4], outer[5]];
    }

    public IReadOnlyList<int> Set(int index) => index switch
    {
        0 => FirstSet,
        1 => [SecondSet],
        _ => ThirdSet,
    };
}
