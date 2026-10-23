using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;
using static AnoMech.Scenarios.M11s.M11sConstants;

namespace AnoMech.Scenarios.M11s.Opener;

public sealed class M11sOpenerState
{
    public const int WeaponSteps = 24;
    private const float Step = MathF.PI / 12f;

    // The opener's Raw Steel Trophy; the second one in the fight is the other.
    public bool AxeTrophyFirst { get; }
    public IReadOnlyList<IReadOnlyList<Weapon>> Weapons { get; }
    public IReadOnlyList<int> FirstWeaponSteps { get; }
    // The first Void Stardust ends in Crushing Comet's stack, the second in Comet's spreads; or the reverse.
    public bool StackFirst { get; }
    public PartyRole CrushingCometTarget { get; }
    public PartyRole HeavyHitterTarget { get; }
    public PartyRole HeavyWeightTarget { get; }

    public M11sOpenerState(Rng rng, M11sOpenerStateOverrides? overrides = null)
    {
        overrides ??= new M11sOpenerStateOverrides();
        var axe = rng.NextBool();
        var first = rng.Shuffle(Enum.GetValues<Weapon>()).ToList();
        var second = rng.Shuffle(Enum.GetValues<Weapon>()).ToList();
        var firstStep = rng.Next(WeaponSteps);
        var secondStep = rng.Next(WeaponSteps);
        var stack = rng.NextBool();
        var comet = rng.NextBool() ? PartyRole.RegenHealer : PartyRole.ShieldHealer;
        var hitter = rng.NextBool() ? PartyRole.RegenHealer : PartyRole.ShieldHealer;
        var weight = rng.NextBool() ? PartyRole.RegenHealer : PartyRole.ShieldHealer;
        AxeTrophyFirst = overrides.AxeTrophyFirst ?? axe;
        Weapons = [overrides.FirstWeapons ?? first, overrides.SecondWeapons ?? second];
        FirstWeaponSteps = [overrides.FirstWeaponStep ?? firstStep, overrides.SecondWeaponStep ?? secondStep];
        StackFirst = overrides.StackFirst ?? stack;
        CrushingCometTarget = overrides.CrushingCometTarget ?? comet;
        HeavyHitterTarget = hitter;
        HeavyWeightTarget = weight;
    }

    // Bearing from the centre; the set goes clockwise from its first weapon, 120° apart.
    public float WeaponBearing(int set, int index) => FirstWeaponSteps[set] * Step - index * MathF.Tau / 3f;

    public Vector3 WeaponSpot(int set, int index) => Toward(WeaponBearing(set, index)) * Geometry.WeaponRing;

    // The Tyrant dashes from the centre, then weapon to weapon; each weapon's area and the party's
    // spots around it turn with the direction it came from.
    public float ArrivalBearing(int set, int index)
        => M11sTrophyWeapons.Bearing(index == 0 ? Vector3.Zero : WeaponSpot(set, index - 1), WeaponSpot(set, index));

    public int IndexOf(int set, Weapon weapon)
    {
        for (var i = 0; i < Weapons[set].Count; i++)
            if (Weapons[set][i] == weapon) return i;
        return -1;
    }

    public bool StackOnSet(int set) => StackFirst == (set == 0);

    public static Vector3 Toward(float bearing) => new(MathF.Sin(bearing), 0f, MathF.Cos(bearing));
}
