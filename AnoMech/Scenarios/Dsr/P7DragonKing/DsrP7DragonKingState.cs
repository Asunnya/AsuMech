using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Dsr.P7DragonKing;

// Flames of Ascalon hits around the Dragon-king, Ice of Ascalon everywhere but there.
public enum AscalonElement
{
    Flames,
    Ice,
}

// The Edge spots around the Dragon-king facing north: front left, front right and behind him.
public enum EdgeSpot
{
    Party,
    SecondParty,
    Tanks,
}

public readonly record struct Explosion(Vector2 At, float Delay);

public sealed class DsrP7DragonKingState
{
    public const int Cycles = 3;
    public const int GigaflareCycles = 2;
    public const float SpotRadius = 8f;
    public const float GigaflareRadius = 14f;
    public const float ExaflareRadius = 6f;
    public const float ExaflareStep = 7f;
    public static readonly float[] ExaflareDelays = [0f, 1.92f, 3.79f, 5.65f, 7.52f, 9.39f];

    private static readonly Vector2[] Spots = [new(-6.93f, -4f), new(6.93f, -4f), new(0f, 8f)];

    public IReadOnlyList<IReadOnlyList<int>> ExaflareHeadings { get; }
    // Exaflare, Akh Morn and Gigaflare of each cycle in turn; the last cycle has no Gigaflare.
    public IReadOnlyList<AscalonElement> Elements { get; }
    // Per Gigaflare cycle, the spots (by EdgeSpot index, mirrored to r14) in blast order.
    public IReadOnlyList<IReadOnlyList<int>> GigaflareOrder { get; }

    // The Dragon-king's facing as his last Edge cast began; every Edge spot turns with it.
    public float Facing { get; set; } = NorthFacing;
    public const float NorthFacing = MathF.PI;

    public DsrP7DragonKingState(Rng rng, DsrP7DragonKingStateOverrides? overrides = null)
    {
        overrides ??= new DsrP7DragonKingStateOverrides();
        var headings = Enumerable.Range(0, Cycles).Select(_ => (IReadOnlyList<int>)[rng.Next(8), rng.Next(8), rng.Next(8)]).ToList();
        ExaflareHeadings = overrides.ExaflareHeadings ?? headings;
        var elements = Enumerable.Range(0, Cycles * 3 - 1).Select(_ => rng.NextBool() ? AscalonElement.Flames : AscalonElement.Ice).ToList();
        Elements = overrides.Elements ?? elements;
        var order = Enumerable.Range(0, GigaflareCycles).Select(_ => (IReadOnlyList<int>)rng.Shuffle(0, 1, 2).ToList()).ToList();
        GigaflareOrder = overrides.GigaflareOrder ?? order;
    }

    public AscalonElement ExaflareElement(int cycle) => Elements[cycle * 3];
    public AscalonElement AkhMornElement(int cycle) => Elements[cycle * 3 + 1];
    public AscalonElement GigaflareElement(int cycle) => Elements[cycle * 3 + 2];

    public static Vector2 Spot(EdgeSpot spot) => Spots[(int)spot];

    // Dark Trinity goes to the top of enmity; that tank holds him from the Edge cast before the pair
    // to the one after it.
    public static PartyRole TopEnmityTank(int trinityPair) => trinityPair % 2 == 0 ? PartyRole.MainTank : PartyRole.OffTank;

    // A spot laid out for a north-facing Dragon-king, turned to where he faces.
    public Vector2 Turned(Vector2 northFacing)
    {
        var turn = Facing - NorthFacing;
        var (sin, cos) = MathF.SinCos(turn);
        return new Vector2(northFacing.X * cos + northFacing.Y * sin, -northFacing.X * sin + northFacing.Y * cos);
    }

    public Vector2 TurnedSpot(EdgeSpot spot) => Turned(Spot(spot));

    public Vector2 GigaflareSpot(int cycle, int blast) => Turned(-Spot((EdgeSpot)GigaflareOrder[cycle][blast]) * (GigaflareRadius / SpotRadius));

    public float ExaflareRotation(int cycle, EdgeSpot spot) => HeadingRotation(ExaflareHeadings[cycle][(int)spot]) + Facing - NorthFacing;

    public IEnumerable<Explosion> ExaflareExplosions(int cycle, EdgeSpot spot)
        => Explosions(TurnedSpot(spot), ExaflareRotation(cycle, spot));

    private static float HeadingRotation(int heading) => heading * MathF.PI / 4f;

    // Each exaflare fires its start, then walks ahead and to both sides one step at a time.
    private static IEnumerable<Explosion> Explosions(Vector2 start, float rotation)
    {
        yield return new Explosion(start, 0f);
        foreach (var turn in new[] { 0f, MathF.PI / 2f, -MathF.PI / 2f })
        {
            var dir = new Vector2(MathF.Sin(rotation + turn), MathF.Cos(rotation + turn));
            for (var k = 1; k < ExaflareDelays.Length; k++)
                yield return new Explosion(start + dir * (ExaflareStep * k), ExaflareDelays[k]);
        }
    }
}
