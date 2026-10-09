using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Dsr.P3Nidhogg;

// Every dive hits its player; the tower it leaves lands 14y ahead (Up), 14y behind (Down) or on them (Circle).
public enum DiveArrow { Circle, Up, Down }

public sealed class DsrP3NidhoggState
{
    public const float ArrowDiveDistance = 14f;
    public const int Dives = 3;

    // Intercardinal tower bearings for TowerSoakers, NE, SE, SW, NW.
    public static readonly float[] TowerBearings = [45f, 135f, 225f, 315f];
    private static readonly int[][] TowerSoakerSets = [[1, 1, 3, 3], [1, 2, 2, 3], [1, 1, 2, 4], [2, 2, 2, 2]];
    private static readonly int[] LineSizes = [3, 2, 3];
    private static readonly PartyRole[] NonTanks =
        [PartyRole.RegenHealer, PartyRole.ShieldHealer, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];

    // Dive from Grace's number, 1-3.
    public IReadOnlyDictionary<PartyRole, int> Lines { get; }
    public IReadOnlyDictionary<PartyRole, DiveArrow> Arrows { get; }
    // Each Lash/Gnash: true when the donut comes first.
    public IReadOnlyList<bool> LashFirst { get; }
    public IReadOnlyList<int> TowerSoakers { get; }
    // The Darkdragon Dive tower that casts no Geirskogul afterwards.
    public int QuietTower { get; }
    public IReadOnlyList<PartyRole> SoulTetherTargets { get; }
    public IReadOnlyList<PartyRole> EyeTargets { get; }
    // Where each dive's tower landed, per dive; set as the dives land.
    public List<(PartyRole Role, System.Numerics.Vector3 Spot)>[] DiveTowers { get; } = [[], [], []];

    public DsrP3NidhoggState(Rng rng, DsrP3NidhoggStateOverrides? overrides = null)
    {
        overrides ??= new DsrP3NidhoggStateOverrides();
        var order = rng.Shuffle(Enum.GetValues<PartyRole>()).ToList();
        var lines = new Dictionary<PartyRole, int>();
        var next = 0;
        for (var line = 1; line <= Dives; line++)
        {
            foreach (var role in order.Skip(next).Take(LineSizes[line - 1]))
                lines[role] = line;
            next += LineSizes[line - 1];
        }
        Lines = overrides.Lines ?? lines;

        // One or two of the three dives carry arrows: one up, one down, the rest circles.
        var arrowedLines = rng.Shuffle(new[] { 1, 2, 3 }).Take(1 + rng.Next(2)).ToHashSet();
        var arrows = new Dictionary<PartyRole, DiveArrow>();
        foreach (var line in Enumerable.Range(1, Dives))
        {
            var members = rng.Shuffle(Lines.Where(kv => kv.Value == line).Select(kv => kv.Key)).ToList();
            for (var i = 0; i < members.Count; i++)
                arrows[members[i]] = !arrowedLines.Contains(line) ? DiveArrow.Circle : i switch { 0 => DiveArrow.Up, 1 => DiveArrow.Down, _ => DiveArrow.Circle };
        }
        Arrows = overrides.Arrows ?? arrows;

        var lashFirst = new[] { rng.NextBool(), rng.NextBool() };
        LashFirst = overrides.LashFirst ?? lashFirst;
        var soakers = rng.Shuffle(TowerSoakerSets[rng.Next(TowerSoakerSets.Length)]).ToList();
        TowerSoakers = overrides.TowerSoakers ?? soakers;
        QuietTower = overrides.QuietTower ?? rng.Next(TowerBearings.Length);
        var tethered = rng.Shuffle(NonTanks).Take(2).ToList();
        SoulTetherTargets = overrides.SoulTetherTargets ?? tethered;
        EyeTargets = [StackTarget(rng, 1), StackTarget(rng, 3)];
    }

    // Eye of the Tyrant stacks on someone not diving in that round.
    private PartyRole StackTarget(Rng rng, int divingLine)
    {
        var candidates = Lines.Where(kv => kv.Value != divingLine).Select(kv => kv.Key).ToList();
        return candidates[rng.Next(candidates.Count)];
    }

    public IEnumerable<PartyRole> Line(int line) => Lines.Where(kv => kv.Value == line).Select(kv => kv.Key).OrderBy(r => r);

    public bool LineHasArrows(int line) => Line(line).Any(r => Arrows[r] != DiveArrow.Circle);

    // How far ahead of the player, along their facing, the dive's tower lands.
    public static float DiveOffset(DiveArrow arrow) => arrow switch
    {
        DiveArrow.Up => ArrowDiveDistance,
        DiveArrow.Down => -ArrowDiveDistance,
        _ => 0f,
    };
}
