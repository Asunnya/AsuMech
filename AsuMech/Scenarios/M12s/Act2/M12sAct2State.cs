using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.M12s.Act2;

// The DPS of each order holds Bonds of Flesh α and goes out; the support holds β and stays in.
public sealed class M12sAct2State
{
    public const int Pairs = 4;
    public const int Spins = 7;
    public const float InnerBlob = 4f;
    public const float OuterBlobX = 15f;
    public const float OuterBlobZ = 10f;

    private static readonly Vector2[] Corners = [new(1f, -1f), new(1f, 1f), new(-1f, 1f), new(-1f, -1f)];

    public IReadOnlyDictionary<PartyRole, int> Order { get; }
    public int FirstBlob { get; }
    public int FirstFacing { get; }

    public M12sAct2State(Rng rng, M12sAct2StateOverrides? overrides = null, PartyRole? player = null)
    {
        overrides ??= new M12sAct2StateOverrides();
        var supports = rng.Shuffle(Enum.GetValues<PartyRole>().Where(r => !r.IsDps())).ToList();
        var dps = rng.Shuffle(Enum.GetValues<PartyRole>().Where(r => r.IsDps())).ToList();
        var firstBlob = rng.Next(4);
        var firstFacing = rng.Next(4);
        if (player is { } me && overrides.PlayerOrder is { } order and >= 1 and <= Pairs)
        {
            var group = me.IsDps() ? dps : supports;
            var at = group.IndexOf(me);
            (group[at], group[order - 1]) = (group[order - 1], group[at]);
        }
        var orders = new Dictionary<PartyRole, int>();
        for (var i = 0; i < Pairs; i++)
        {
            orders[supports[i]] = i + 1;
            orders[dps[i]] = i + 1;
        }
        Order = orders;
        FirstBlob = overrides.FirstBlob ?? firstBlob;
        FirstFacing = overrides.FirstFacing ?? firstFacing;
    }

    public PartyRole Alpha(int order) => Order.First(kv => kv.Value == order && kv.Key.IsDps()).Key;
    public PartyRole Beta(int order) => Order.First(kv => kv.Value == order && !kv.Key.IsDps()).Key;

    // Rotation of the boss on spin 1..7: it turns a quarter counter-clockwise each spin.
    public float Facing(int spin) => (FirstFacing switch { 0 => MathF.PI, 1 => MathF.PI / 2f, 2 => 0f, _ => -MathF.PI / 2f }) + (spin - 1) * MathF.PI / 2f;

    public static Vector2 Direction(float rotation) => new(MathF.Round(MathF.Sin(rotation), 5), MathF.Round(MathF.Cos(rotation), 5));

    // The β of order k stands where the boss faces on spin k; the α leaves through the gap two spins on.
    public Vector2 BetaDirection(int order) => Direction(Facing(order));
    public Vector2 AlphaDirection(int order) => Direction(Facing(order + 2));

    // Blobs 1-4: two inside the coil in opposite corners, then two outside it.
    public Vector2 BlobSpot(int blob)
    {
        var corner = Corners[FirstBlob];
        var outer = new Vector2(-corner.X * OuterBlobX, corner.Y * OuterBlobZ);
        return blob switch
        {
            1 => corner * InnerBlob,
            2 => -corner * InnerBlob,
            3 => outer,
            _ => -outer,
        };
    }

    // The order whose α soaks blob tower k, and whose β soaks chain tower k.
    public static int SoakerOrder(int tower) => (tower + 1) % Pairs + 1;
}
