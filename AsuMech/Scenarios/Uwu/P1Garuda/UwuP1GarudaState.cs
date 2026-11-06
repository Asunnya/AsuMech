using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;

using AsuMech.Scenarios;

namespace AsuMech.Scenarios.Uwu.P1Garuda;

// The opening Mistral Song always targets a healer.
public sealed class UwuP1GarudaState
{
    public static readonly Vector3 SpinyPlumeSpawn = new(-10f, 0f, 0f);
    public static readonly Vector3 GigastormSpot = new(-6.8f, 0f, 6.2f);
    public const float SongSpotRadius = 19.5f;
    public static readonly Vector3 SuparnaTetherSpot = new(-15f, 0f, 0f);
    public static readonly Vector3 ChiradaTetherSpot = new(15f, 0f, 0f);

    public const float BubbleRadius = 6f;

    public static readonly Vector3[] SatinPlumeSpots =
    [
        new(-4f, 0f, -16f), new(4f, 0f, -16f), new(12f, 0f, -12f), new(16f, 0f, -4f),
        new(16f, 0f, 4f), new(12f, 0f, 12f), new(4f, 0f, 16f), new(-4f, 0f, 16f),
        new(-12f, 0f, 12f), new(-16f, 0f, 4f), new(-16f, 0f, -4f), new(-12f, 0f, -12f),
    ];

    private static readonly PartyRole[] Healers = [PartyRole.RegenHealer, PartyRole.ShieldHealer];

    private static readonly PartyRole[] NonTanks =
    [
        PartyRole.RegenHealer, PartyRole.ShieldHealer, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB,
        PartyRole.PhysRangedDps, PartyRole.CasterDps,
    ];

    public Vector3 SuparnaSongSpot { get; }
    public Vector3 ChiradaSongSpot { get; }
    public PartyRole MistralSongTarget { get; }
    public IReadOnlyList<PartyRole> SistersSongTargets { get; }
    public IReadOnlyList<PartyRole> FrictionTargets { get; }
    public IReadOnlyList<Vector3> SatinPlumesFirst { get; }
    public IReadOnlyList<Vector3> SatinPlumesSecond { get; }
    public IReadOnlyList<PartyRole> MesohighTargets { get; }

    public Vector3 SuparnaSongIntercept { get; set; }
    public Vector3 ChiradaSongIntercept { get; set; }
    public SimTether? SuparnaMesohigh { get; set; }
    public SimTether? ChiradaMesohigh { get; set; }

    public UwuP1GarudaState(Rng rng, UwuP1GarudaStateOverrides? overrides = null)
    {
        var forced = overrides ?? new UwuP1GarudaStateOverrides();
        // Every roll is drawn before a pin replaces it, so the seed's other rolls stay put.
        var cardinals = rng.Shuffle(0f, 90f, 180f, 270f).Take(2).ToArray();
        SuparnaSongSpot = AtBearing(forced.SuparnaSongBearing ?? cardinals[0], SongSpotRadius);
        ChiradaSongSpot = AtBearing(forced.ChiradaSongBearing ?? cardinals[1], SongSpotRadius);
        var songTarget = Healers[rng.Next(Healers.Length)];
        MistralSongTarget = forced.MistralSongTarget ?? songTarget;
        var sistersTargets = rng.Shuffle(NonTanks).Take(2).ToList();
        SistersSongTargets = forced.SistersSongTargets ?? sistersTargets;
        var frictionTargets = new List<PartyRole> { NonTanks[rng.Next(NonTanks.Length)], NonTanks[rng.Next(NonTanks.Length)] };
        FrictionTargets = forced.FrictionTargets ?? frictionTargets;
        var plumesFirst = rng.Shuffle(SatinPlumeSpots).Take(4).ToList();
        SatinPlumesFirst = Plumes(forced.SatinPlumesFirst) ?? plumesFirst;
        var plumesSecond = rng.Shuffle(SatinPlumeSpots).Take(4).ToList();
        SatinPlumesSecond = Plumes(forced.SatinPlumesSecond) ?? plumesSecond;
        var mesohighTargets = rng.Shuffle(Enum.GetValues<PartyRole>()).Take(2).ToList();
        MesohighTargets = forced.MesohighTargets ?? mesohighTargets;
    }

    // Pinned plumes snap to the nearest of the twelve spawn spots; fewer than four means no pin.
    private static IReadOnlyList<Vector3>? Plumes(List<Vector2>? pinned)
        => pinned is { Count: 4 } ? pinned.Select(p => SatinPlumeSpots.MinBy(s => Vector2.Distance(new Vector2(s.X, s.Z), p))).ToList() : null;

    // Compass degrees: 0 = north (-Z), 90 = east (+X).
    public static Vector3 AtBearing(float bearingDegrees, float radius)
    {
        var rad = bearingDegrees * MathF.PI / 180f;
        return new Vector3(radius * MathF.Sin(rad), 0f, -radius * MathF.Cos(rad));
    }

    public static float BearingOf(Vector3 point) => (MathF.Atan2(point.X, -point.Z) * 180f / MathF.PI + 360f) % 360f;
}
