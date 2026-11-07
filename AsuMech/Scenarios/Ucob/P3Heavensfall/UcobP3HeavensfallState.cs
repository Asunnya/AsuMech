using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Ucob.P3Heavensfall;

// Where Nael lands against the other two, all three a step apart on the edge.
public enum HeavensfallFormation { NaelBetween, NaelCounterClockwise, NaelClockwise }

public sealed class UcobP3HeavensfallState
{
    public const float NaelDistance = 24f;
    public const float DiverDistance = 23.57f;
    public const float TowerDistance = 22f;
    public const int TowerSlots = 16;
    public const int Towers = 8;
    public const float TowerSlotDegrees = 360f / TowerSlots;
    public const int Twisters = 4;
    public const int MegaflarePuddles = 4;
    public const float BurstStepDegrees = 22.5f;

    public HeavensfallFormation Formation { get; }
    // UNVERIFIED: Nael was only seen north; any intercardinal or cardinal is assumed.
    public float NaelBearing { get; }
    // Tower slots counted clockwise from the one under Nael, which always has one. UNVERIFIED.
    public IReadOnlyList<int> TowerSlotsTaken { get; }
    public PartyRole MegaflareDiveTarget { get; }
    public PartyRole TwistingDiveTarget { get; }
    public IReadOnlyList<PartyRole> TwisterTargets { get; }
    // UNVERIFIED: any four.
    public IReadOnlyList<PartyRole> MegaflarePuddleTargets { get; }
    public PartyRole HypernovaTarget { get; }
    public PartyRole FireballTarget { get; }
    // UNVERIFIED: only a clockwise sweep was seen.
    public bool BurstsClockwise { get; }

    public UcobP3HeavensfallState(Rng rng, UcobP3HeavensfallStateOverrides? overrides = null)
    {
        overrides ??= new UcobP3HeavensfallStateOverrides();
        var roles = Enum.GetValues<PartyRole>();
        var formation = rng.NextObj(Enum.GetValues<HeavensfallFormation>());
        var naelStep = rng.Next(8);
        var towers = new[] { 0 }.Concat(rng.Shuffle(Enumerable.Range(1, TowerSlots - 1)).Take(Towers - 1)).OrderBy(s => s).ToList();
        var divers = Roll(rng, 2, overrides.DiveIncludes);
        var twisters = Roll(rng, Twisters, overrides.TwisterIncludes);
        var puddles = Roll(rng, MegaflarePuddles, overrides.MegaflarePuddleIncludes);
        var hypernova = rng.NextObj(roles);
        var fireball = rng.NextObj(roles);
        var clockwise = rng.NextBool();
        Formation = overrides.Formation ?? formation;
        NaelBearing = 45f * (overrides.NaelStep ?? naelStep);
        TowerSlotsTaken = overrides.TowerSlots ?? towers;
        MegaflareDiveTarget = divers[0];
        TwistingDiveTarget = divers[1];
        TwisterTargets = twisters;
        MegaflarePuddleTargets = puddles;
        HypernovaTarget = overrides.HypernovaTarget ?? hypernova;
        FireballTarget = overrides.FireballTarget ?? fireball;
        BurstsClockwise = overrides.BurstsClockwise ?? clockwise;
    }

    private static IReadOnlyList<PartyRole> Roll(Rng rng, int count, PartyRole? include)
    {
        var pool = rng.Shuffle(Enum.GetValues<PartyRole>()).ToList();
        if (include is { } role)
        {
            pool.Remove(role);
            pool.Insert(0, role);
        }
        return pool.Take(count).ToList();
    }

    public float BahamutBearing => NaelBearing + Formation switch
    {
        HeavensfallFormation.NaelBetween => -45f,
        HeavensfallFormation.NaelCounterClockwise => 45f,
        _ => -90f,
    };

    public float TwintaniaBearing => NaelBearing + Formation switch
    {
        HeavensfallFormation.NaelBetween => 45f,
        HeavensfallFormation.NaelCounterClockwise => 90f,
        _ => -45f,
    };

    public float TowerBearing(int slot) => NaelBearing + slot * TowerSlotDegrees;

    public float FirstBurstBearing => NaelBearing + (BurstsClockwise ? 45f : -45f);

    public static Vector2 OnBearing(float degrees, float distance)
    {
        var r = degrees * MathF.PI / 180f;
        return new Vector2(MathF.Sin(r), -MathF.Cos(r)) * distance;
    }

    public static float FacingOf(Vector2 from, Vector2 to) => MathF.Atan2(to.X - from.X, to.Y - from.Y);

    // FFXIV's rotation for a cone aimed along `degrees`.
    public static float RotationOfBearing(float degrees) => MathF.PI - degrees * MathF.PI / 180f;
}
