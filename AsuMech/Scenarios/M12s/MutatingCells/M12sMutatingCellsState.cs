using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.M12s.MutatingCells;

public enum OrbShape
{
    Water,
    Aero,
    Thunder,
    Fire,
}

public sealed record Orb(OrbShape Shape, int Side, Vector3 Destination);

public sealed class M12sMutatingCellsState
{
    public const float HoleDistance = 10f;
    private static readonly Vector2 FarOffset = new(5.08f, 10.88f);
    private static readonly Vector2 CloseOffset = new(3.42f, 4.92f);
    private static readonly (int X, int Z)[] Corners = [(-1, -1), (1, -1), (1, 1), (-1, 1)];

    // One tank, one healer and two DPS get each mutation.
    public IReadOnlySet<PartyRole> AlphaFirst { get; }
    public int CloseSide { get; }
    // UNVERIFIED: only water with thunder (or aero with fire) close, which keeps every wave dodgeable.
    public bool CloseWaterThunder { get; }
    // Four on each side; on the close side the close pair sits on one diagonal at half the distance.
    public IReadOnlyList<Orb> Orbs { get; }
    public bool NetherworldFar { get; }
    public int SobatTurn { get; }

    public M12sMutatingCellsState(Rng rng, M12sMutatingCellsStateOverrides? overrides = null, PartyRole? player = null)
    {
        overrides ??= new M12sMutatingCellsStateOverrides();
        PartyRole[][] pairs =
        [
            [PartyRole.MainTank, PartyRole.OffTank],
            [PartyRole.RegenHealer, PartyRole.ShieldHealer],
        ];
        var alpha = pairs.Select(p => p[rng.Next(2)]).Concat(rng.Shuffle(PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps).Take(2)).ToHashSet();
        if (player is { } me && overrides.PlayerAlphaFirst is { } wantsAlpha && alpha.Contains(me) != wantsAlpha)
        {
            var swap = SameKind(me).First(r => r != me && alpha.Contains(r) == wantsAlpha);
            alpha = alpha.Select(r => r == me ? swap : r == swap ? me : r).ToHashSet();
        }
        AlphaFirst = alpha;

        var side = rng.NextSign();
        CloseSide = overrides.CloseSide ?? side;
        var waterThunder = rng.NextBool();
        CloseWaterThunder = overrides.CloseWaterThunder ?? waterThunder;

        var shapes = Enum.GetValues<OrbShape>();
        var farShapes = rng.Shuffle(shapes).ToList();
        var close = ClosePair(CloseWaterThunder);
        var closeShapes = rng.Shuffle(close).ToList();
        var otherShapes = rng.Shuffle(shapes.Except(close)).ToList();
        var diagonal = rng.Next(2);
        var orbs = new List<Orb>();
        for (var c = 0; c < 4; c++) orbs.Add(new Orb(farShapes[c], -CloseSide, At(-CloseSide, Corners[c], FarOffset)));
        for (var k = 0; k < 2; k++)
        {
            orbs.Add(new Orb(closeShapes[k], CloseSide, At(CloseSide, Corners[diagonal + 2 * k], CloseOffset)));
            orbs.Add(new Orb(otherShapes[k], CloseSide, At(CloseSide, Corners[1 - diagonal + 2 * k], FarOffset)));
        }
        Orbs = orbs;

        var far = rng.NextBool();
        NetherworldFar = overrides.NetherworldFar ?? far;
        var turn = rng.Next(4);
        SobatTurn = overrides.SobatTurn ?? turn;
    }

    private static IEnumerable<PartyRole> SameKind(PartyRole role) => role switch
    {
        PartyRole.MainTank or PartyRole.OffTank => [PartyRole.MainTank, PartyRole.OffTank],
        PartyRole.RegenHealer or PartyRole.ShieldHealer => [PartyRole.RegenHealer, PartyRole.ShieldHealer],
        _ => [PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps],
    };

    public static OrbShape[] ClosePair(bool waterThunder) => waterThunder ? [OrbShape.Water, OrbShape.Thunder] : [OrbShape.Aero, OrbShape.Fire];

    public static Vector3 Hole(int side) => new(side * HoleDistance, 0f, 0f);

    private static Vector3 At(int side, (int X, int Z) corner, Vector2 offset) => Hole(side) + new Vector3(corner.X * offset.X, 0f, corner.Z * offset.Y);

    public bool IsAlpha(PartyRole role, int round) => AlphaFirst.Contains(role) == (round == 0);

    // The hole whose first wave is aero and fire, safe under it; the other hole's is next.
    public int FirstSafeSide => CloseWaterThunder ? -CloseSide : CloseSide;

    // The far side's orbs that match the close pair: popping them sends them to the second wave.
    public IEnumerable<Orb> OrbsToPop => Orbs.Where(o => o.Side != CloseSide && ClosePair(CloseWaterThunder).Contains(o.Shape));

    public float SobatTurnRadians => SobatTurn switch { 1 => MathF.PI / 2f, 2 => MathF.PI, 3 => -MathF.PI / 2f, _ => 0f };
}
