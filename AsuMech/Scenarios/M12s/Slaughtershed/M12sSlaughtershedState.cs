using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.M12s.Slaughtershed;

// One of Mortal Slayer's orbs: the side of the arena it hits (east: x > 0) and its colour.
public sealed record SlayerOrb(bool East, bool Purple);

// A Slaughtershed's arms: Serpentine Scourge cleaves half the room, Raptor Knuckles knocks back
// from a north corner; each hits one side, then the other.
public sealed record Arms(bool Serpentine, bool WestFirst);

public sealed class M12sSlaughtershedState
{
    public const int Pairs = 4;
    public const int Rounds = 3;

    public static readonly PartyRole[] Supports = [PartyRole.MainTank, PartyRole.OffTank, PartyRole.RegenHealer, PartyRole.ShieldHealer];
    public static readonly PartyRole[] Dps = [PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];

    // Four orbs fall on each side, both purples on the same one, and a pair never drops a purple
    // and a green on one side.
    public bool PurpleEast { get; }
    public IReadOnlyList<IReadOnlyList<SlayerOrb>> Orbs { get; }

    // Each Slaughtershed one role spreads and one member of the other takes the stack.
    public IReadOnlyList<bool> SupportsSpread { get; }
    public IReadOnlyList<PartyRole> StackTargets { get; }
    // The blood vessel that moves between rounds sits east (10.75, -3.5) or west.
    public IReadOnlyList<bool> BlobEast { get; }
    // UNVERIFIED: one round of three has the cleaves, as both logged pulls did.
    public IReadOnlyList<Arms> ArmsOf { get; }

    public M12sSlaughtershedState(Rng rng, M12sSlaughtershedStateOverrides? overrides = null, PartyRole? player = null)
    {
        overrides ??= new M12sSlaughtershedStateOverrides();
        var purpleEast = rng.NextBool();
        var sides = rng.Shuffle(true, true, true, true, false, false, false, false).ToList();
        PurpleEast = overrides.PurpleEast ?? purpleEast;
        var choices = PurpleChoices(sides, PurpleEast).ToList();
        var purples = choices[rng.Next(choices.Count)];
        Orbs = Enumerable.Range(0, Pairs)
            .Select(p => (IReadOnlyList<SlayerOrb>)Enumerable.Range(0, 2)
                .Select(i => new SlayerOrb(sides[p * 2 + i], purples.Contains(p * 2 + i)))
                .ToList())
            .ToList();

        var spread = new List<bool>();
        var stack = new List<PartyRole>();
        var blob = new List<bool>();
        var serpentine = rng.Next(Rounds);
        var arms = new List<Arms>();
        for (var r = 0; r < Rounds; r++)
        {
            var supportsSpread = rng.NextBool();
            if (player is { } me && overrides.PlayerSpreads is { } meSpread) supportsSpread = meSpread == !me.IsDps();
            spread.Add(supportsSpread);
            var stackers = supportsSpread ? Dps : Supports;
            stack.Add(stackers[rng.Next(stackers.Length)]);
            blob.Add(rng.NextBool());
            arms.Add(new Arms(false, rng.NextBool()));
        }
        serpentine = overrides.SerpentineRound ?? serpentine;
        arms[serpentine] = arms[serpentine] with { Serpentine = true };
        SupportsSpread = spread;
        StackTargets = stack;
        BlobEast = blob;
        ArmsOf = arms;
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

    public int PurpleSide => PurpleEast ? 1 : -1;

    public static readonly float[] OrbX = [15f, 14.5f, 10f, 5.5f];
    public static readonly float[] OrbZ = [-31f, -34f, -31f, -29f];

    // Each side's orbs come down along the same four spots, outermost first.
    public Vector3 OrbSpot(int pair, int index)
    {
        var orb = Orbs[pair][index];
        var n = Enumerable.Range(0, pair * 2 + index).Count(k => Orbs[k / 2][k % 2].East == orb.East);
        return new Vector3((orb.East ? 1f : -1f) * OrbX[n], 0f, OrbZ[n]);
    }

    // A pair's orbs on one side, outermost first: the outer one takes the player nearest the boss.
    public IReadOnlyList<int> FallingOn(int pair, bool east)
        => Enumerable.Range(0, 2).Where(i => Orbs[pair][i].East == east).OrderByDescending(i => MathF.Abs(OrbSpot(pair, i).X)).ToList();

    public bool Spreads(int round, PartyRole role) => SupportsSpread[round] == !role.IsDps();

    private static readonly Vector3[] FixedBlobs = [new(-13.5f, 0f, 5.9f), new(2.5f, 0f, 6.4f), new(18.5f, 0f, 6.16f), new(0.8f, 0f, -8f)];

    public IReadOnlyList<Vector3> Blobs(int round) => [new((BlobEast[round] ? 1f : -1f) * 10.75f, 0f, -3.5f), .. FixedBlobs];
}
