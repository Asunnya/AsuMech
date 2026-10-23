using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.M11s.UltimateWeapons;

public sealed class M11sUltimateWeaponsState
{
    public const int Strikes = 6;
    // From the third weapon on, each leaves a Maelstrom where it struck.
    public const int FirstTornadoWeapon = 2;

    // The six spots Ultimate Trophy Weapons always uses, in strike order, first weapon east.
    private static readonly Vector3[] EastSpots =
    [
        new(6f, 0f, 10.39f), new(10.4f, 0f, -6.04f), new(-8.92f, 0f, -0.86f),
        new(-1.43f, 0f, 12.12f), new(10.59f, 0f, 0.11f), new(0f, 0f, -12.03f),
    ];

    public int OpenSide { get; }
    public bool AxeTrophy { get; }
    public bool Mirrored { get; }
    // Each half, the first three and the last three, holds one of every weapon.
    public IReadOnlyList<Weapon> Weapons { get; }
    public PartyRole HeavyHitterTarget { get; }
    public PartyRole HeavyWeightTarget { get; }

    public static readonly PartyRole[] EyeTargets = [PartyRole.MainTank, PartyRole.OffTank, PartyRole.RegenHealer, PartyRole.ShieldHealer];

    public M11sUltimateWeaponsState(Rng rng, M11sUltimateWeaponsStateOverrides? overrides = null)
    {
        overrides ??= new M11sUltimateWeaponsStateOverrides();
        var side = rng.Next(4);
        var axe = rng.NextBool();
        var mirrored = rng.NextBool();
        var weapons = rng.Shuffle(Enum.GetValues<Weapon>()).Concat(rng.Shuffle(Enum.GetValues<Weapon>())).ToList();
        var hitter = rng.NextBool() ? PartyRole.RegenHealer : PartyRole.ShieldHealer;
        var weight = rng.NextBool() ? PartyRole.RegenHealer : PartyRole.ShieldHealer;
        OpenSide = overrides.OpenSide ?? side;
        AxeTrophy = overrides.AxeTrophy ?? axe;
        Mirrored = overrides.Mirrored ?? mirrored;
        Weapons = overrides.Weapons ?? weapons;
        HeavyHitterTarget = hitter;
        HeavyWeightTarget = weight;
    }

    public float OpenBearing => OpenSide * MathF.PI / 2f;

    public Vector3 WeaponSpot(int index) => Mirrored ? EastSpots[index] with { X = -EastSpots[index].X } : EastSpots[index];

    public float ArrivalBearing(int index)
        => M11sTrophyWeapons.Bearing(index == 0 ? Vector3.Zero : WeaponSpot(index - 1), WeaponSpot(index));

    public IEnumerable<Vector3> TornadoSpots() => Enumerable.Range(FirstTornadoWeapon, Strikes - FirstTornadoWeapon).Select(WeaponSpot);

    // Turns a spot authored with the open side south onto the rolled one.
    public Vector3 FromSouthOpen(Vector3 p)
    {
        var (sin, cos) = MathF.SinCos(OpenBearing);
        return new Vector3(p.X * cos + p.Z * sin, 0f, p.Z * cos - p.X * sin);
    }
}
