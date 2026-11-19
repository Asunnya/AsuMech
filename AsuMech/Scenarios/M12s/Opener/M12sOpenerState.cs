using System.Collections.Generic;
using System.Linq;

namespace AsuMech.Scenarios.M12s.Opener;

// One of Mortal Slayer's orbs: the side of the arena it hits (east: x > 0) and its colour.
public sealed record MortalOrb(bool East, bool Purple);

public sealed class M12sOpenerState
{
    public const int Pairs = 4;

    // Four orbs fall on each side; both purples fall on the same side, and a pair never drops a
    // purple and a green on one side.
    public bool PurpleEast { get; }
    public IReadOnlyList<IReadOnlyList<MortalOrb>> Orbs { get; }

    public M12sOpenerState(Rng rng, M12sOpenerStateOverrides? overrides = null)
    {
        overrides ??= new M12sOpenerStateOverrides();
        var purpleEast = rng.NextBool();
        var sides = rng.Shuffle(true, true, true, true, false, false, false, false).ToList();
        var choices = PurpleChoices(sides, overrides.PurpleEast ?? purpleEast).ToList();
        var purples = choices[rng.Next(choices.Count)];
        PurpleEast = overrides.PurpleEast ?? purpleEast;
        Orbs = overrides.Orbs ?? Enumerable.Range(0, Pairs)
            .Select(p => (IReadOnlyList<MortalOrb>)Enumerable.Range(0, 2)
                .Select(i => new MortalOrb(sides[p * 2 + i], purples.Contains(p * 2 + i)))
                .ToList())
            .ToList();
    }

    private static IEnumerable<int[]> PurpleChoices(IReadOnlyList<bool> sides, bool purpleEast)
    {
        var onSide = Enumerable.Range(0, sides.Count).Where(i => sides[i] == purpleEast).ToList();
        for (var a = 0; a < onSide.Count; a++)
            for (var b = a + 1; b < onSide.Count; b++)
            {
                int[] chosen = [onSide[a], onSide[b]];
                if (onSide.All(i => sides[i ^ 1] != purpleEast || chosen.Contains(i) == chosen.Contains(i ^ 1)))
                    yield return chosen;
            }
    }

    public IEnumerable<MortalOrb> OrbsOn(int pair, bool east) => Orbs[pair].Where(o => o.East == east);
}
