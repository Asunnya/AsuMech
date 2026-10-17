using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.M10s.Opener;

public sealed class M10sOpenerState
{
    public const int Floaters = 4;

    public int Wall { get; }
    public IReadOnlyList<PartyRole> FloaterOrder { get; }
    public PartyRole PyrotationTarget { get; }

    // Set once Cutback Blaze aims: the bearing, from Red Hot, of the slice it spares.
    public float? SafeSliceRotation { get; set; }
    public Vector3 CutbackOrigin { get; set; }
    // Where each Pyrotation stack landed.
    public List<Vector3> PyrotationSpots { get; } = [];
    public List<Vector3> InfernoPuddles { get; } = [];

    public M10sOpenerState(Rng rng, M10sOpenerStateOverrides? overrides = null, PartyRole? player = null)
    {
        overrides ??= new M10sOpenerStateOverrides();
        var wall = rng.Next(4);
        Wall = overrides.Wall ?? wall;
        var order = rng.Shuffle(Enum.GetValues<PartyRole>()).Take(Floaters).ToList();
        if (player is { } me && overrides.PlayerFloaterNumber is { } wanted and >= 0 and <= Floaters)
        {
            order = rng.Shuffle(Enum.GetValues<PartyRole>().Where(r => r != me)).Take(wanted == 0 ? Floaters : Floaters - 1).ToList();
            if (wanted > 0) order.Insert(wanted - 1, me);
        }
        FloaterOrder = overrides.FloaterOrder ?? order;
        var healer = rng.NextBool() ? PartyRole.RegenHealer : PartyRole.ShieldHealer;
        PyrotationTarget = overrides.PyrotationTarget ?? healer;
    }

    public int FloaterNumber(PartyRole role)
    {
        for (var i = 0; i < FloaterOrder.Count; i++)
            if (FloaterOrder[i] == role) return i + 1;
        return 0;
    }

    // Unit vector from the centre to the wall Red Hot leaps to.
    public Vector2 WallDirection => Wall switch
    {
        0 => new Vector2(0f, -1f),
        1 => new Vector2(1f, 0f),
        2 => new Vector2(0f, 1f),
        _ => new Vector2(-1f, 0f),
    };

    // Spots authored with Red Hot on the west wall, turned onto the rolled wall.
    public Vector3 FromWestFrame(Vector2 local)
    {
        var c = -WallDirection.X;
        var s = -WallDirection.Y;
        return new Vector3(c * local.X - s * local.Y, 0f, s * local.X + c * local.Y);
    }
}
