using System;
using System.Linq;
using System.Numerics;

namespace AsuMech.Scenarios.Fru.IntermissionIceVeil;

// The order the party brings the four Crystals of Light down in.
//
// Simplification: the sim deals no damage, so the crystals and the Ice Veil fall on the clock of a
// clearing party. A Crystal of Light a Sinbound Blizzard reached never falls.
public sealed class FruIceVeilState
{
    public const float CrystalsSpawnAt = 33.93f;
    public const float CrystalsTetheredAt = 37.83f;
    public const float EndlessIceAgeAt = 38.79f;
    public const float LightTethersAt = 49.79f;
    public const float IcecrusherAt = 74.6f;
    // Retail's falls as Endless Ice Age's bar ends; a second sooner here, so the cast is cut.
    public const float VeilFallsAt = 77.9f;
    public const float LightRadius = 15f;
    public const float DarkRadius = 8f;

    public static readonly float[] StormAt = [44.82f, 48.27f, 51.55f, 54.86f, 58.16f, 61.46f];
    public static readonly float[] BlizzardAt = [43.93f, 49.32f, 54.7f, 60.12f, 65.41f, 70.72f];
    public static readonly float[] HiemalRayAt = [54.65f, 56.58f, 58.67f];
    private static readonly float[] FallTimes = [58.55f, 59.18f, 61.42f, 63.37f];

    // Light i stands on cardinal i clockwise from north; dark i on the intercard after it.
    private readonly int[] fallOrder;

    public FruIceVeilState(Rng rng)
    {
        fallOrder = rng.Shuffle(Enumerable.Range(0, 4).ToArray()).ToArray();
    }

    public float LightFallsAt(int light) => FallTimes[Array.IndexOf(fallOrder, light)];
    public static float LastLightFallsAt => FallTimes[^1];

    public static Vector3 Light(int light) => Point(light * 2, LightRadius);
    public static Vector3 Dark(int dark) => Point(dark * 2 + 1, DarkRadius);

    // Octants clockwise from north.
    public static Vector3 Point(float octant, float radius)
        => new(MathF.Sin(octant * MathF.PI / 4) * radius, 0, -MathF.Cos(octant * MathF.PI / 4) * radius);
}
