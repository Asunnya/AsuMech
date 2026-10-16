using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Dsr.P6DoubleDragons;

public enum BreathGlow
{
    Both,
    Nidhogg,
    Hraesvelgr,
}

public sealed class DsrP6DoubleDragonsState
{
    public const float DiveLaneOffset = 11f;
    public const float DiveStartDistance = 34f;
    public const float OrbSpacing = 6f;
    public const float CornerSetCentre = 13f;

    public IReadOnlyCollection<PartyRole> NidhoggTethers { get; }
    public BreathGlow Glow { get; }
    public PartyRole VowTarget { get; }
    public bool HraesvelgrStackOnRegen { get; }
    // Hallowed Wings cleaves the north half when true, the south half otherwise.
    public bool WingsNorth { get; }
    // Hraesvelgr's head down: Hallowed Plume takes the two closest to him, else the two farthest.
    public bool PlumesNear { get; }
    public bool NidhoggDivesEast { get; }
    public bool NidhoggFromSouth { get; }
    public IReadOnlyCollection<PartyRole> Spreads { get; }
    public IReadOnlyCollection<PartyRole> Stacks { get; }
    public PartyRole AkhMornTarget { get; }
    // -1 west, 0 middle, 1 east.
    public int HraesvelgrLane { get; }
    public bool HraesvelgrFromSouth { get; }
    // Corner of the second orb set as (x sign, z sign); the third set takes the opposite corner.
    public (int X, int Z) SecondSetCorner { get; }
    // For each of the three sets, the column offset index of the orb in each row.
    public IReadOnlyList<IReadOnlyList<int>> OrbColumns { get; }
    public bool HotWing { get; }
    public IReadOnlyCollection<PartyRole> SecondNidhoggTethers { get; }
    public BreathGlow SecondGlow { get; }
    public bool SecondHraesvelgrStackOnRegen { get; }
    public bool SecondWingsNorth { get; }
    public bool SecondPlumesNear { get; }
    public bool SecondHotWing { get; }
    // Nidhogg takes the west half of the north wall for the closing Cauterize, Hraesvelgr the east.
    public bool NidhoggCauterizesWest { get; }

    public DsrP6DoubleDragonsState(Rng rng, DsrP6DoubleDragonsStateOverrides? overrides = null)
    {
        overrides ??= new DsrP6DoubleDragonsStateOverrides();
        var nonTanks = Enum.GetValues<PartyRole>().Where(r => !r.IsTank()).ToList();
        var nidhoggTethers = rng.Shuffle(nonTanks).Take(3).ToList();
        NidhoggTethers = overrides.NidhoggTethers ?? nidhoggTethers;
        var glow = (BreathGlow)rng.Next(3);
        Glow = overrides.Glow ?? glow;
        var dps = Enum.GetValues<PartyRole>().Where(r => r.IsDps()).ToList();
        var vow = dps[rng.Next(dps.Count)];
        VowTarget = overrides.VowTarget ?? vow;
        var stackOnRegen = rng.NextBool();
        HraesvelgrStackOnRegen = overrides.HraesvelgrStackOnRegen ?? stackOnRegen;
        var wingsNorth = rng.NextBool();
        WingsNorth = overrides.WingsNorth ?? wingsNorth;
        var plumesNear = rng.NextBool();
        PlumesNear = overrides.PlumesNear ?? plumesNear;
        var divesEast = rng.NextBool();
        NidhoggDivesEast = overrides.NidhoggDivesEast ?? divesEast;
        var nidhoggSouth = rng.NextBool();
        NidhoggFromSouth = overrides.NidhoggFromSouth ?? nidhoggSouth;
        var wroth = rng.Shuffle(Enum.GetValues<PartyRole>()).ToList();
        Spreads = overrides.Spreads ?? wroth.Take(4).ToList();
        Stacks = overrides.Stacks ?? wroth.Except(Spreads).Take(2).ToList();
        var akhMorn = (PartyRole)rng.Next(8);
        AkhMornTarget = overrides.AkhMornTarget ?? akhMorn;
        var lane = rng.Next(3) - 1;
        HraesvelgrLane = overrides.HraesvelgrLane ?? lane;
        var hraesSouth = rng.NextBool();
        HraesvelgrFromSouth = overrides.HraesvelgrFromSouth ?? hraesSouth;
        var corner = (rng.NextBool() ? 1 : -1, rng.NextBool() ? 1 : -1);
        SecondSetCorner = overrides.SecondSetCorner ?? corner;
        OrbColumns = Enumerable.Range(0, 3).Select(_ => (IReadOnlyList<int>)rng.Shuffle(new[] { 0, 1, 2 }).ToList()).ToList();
        var hotWing = rng.NextBool();
        HotWing = overrides.HotWing ?? hotWing;
        var secondTethers = rng.Shuffle(nonTanks).Take(3).ToList();
        SecondNidhoggTethers = overrides.SecondNidhoggTethers ?? secondTethers;
        var secondGlow = (BreathGlow)rng.Next(3);
        SecondGlow = overrides.SecondGlow ?? secondGlow;
        var secondStack = rng.NextBool();
        SecondHraesvelgrStackOnRegen = overrides.SecondHraesvelgrStackOnRegen ?? secondStack;
        var secondWings = rng.NextBool();
        SecondWingsNorth = overrides.SecondWingsNorth ?? secondWings;
        var secondPlumes = rng.NextBool();
        SecondPlumesNear = overrides.SecondPlumesNear ?? secondPlumes;
        var secondHotWing = rng.NextBool();
        SecondHotWing = overrides.SecondHotWing ?? secondHotWing;
        var nidhoggWest = rng.NextBool();
        NidhoggCauterizesWest = overrides.NidhoggCauterizesWest ?? nidhoggWest;
    }

    public IReadOnlyCollection<PartyRole> NidhoggTethersAt(int wyrmsbreath) => wyrmsbreath == 0 ? NidhoggTethers : SecondNidhoggTethers;
    public BreathGlow GlowAt(int wyrmsbreath) => wyrmsbreath == 0 ? Glow : SecondGlow;
    public bool WingsNorthAt(int wings) => wings == 0 ? WingsNorth : SecondWingsNorth;
    public bool PlumesNearAt(int wings) => wings == 0 ? PlumesNear : SecondPlumesNear;
    public bool HotWingAt(int hotWing) => hotWing == 0 ? HotWing : SecondHotWing;

    public IEnumerable<PartyRole> HraesvelgrTethersAt(int wyrmsbreath) => Enum.GetValues<PartyRole>().Where(r => !r.IsTank() && !NidhoggTethersAt(wyrmsbreath).Contains(r));

    public static bool NidhoggGlows(BreathGlow glow) => glow is BreathGlow.Both or BreathGlow.Nidhogg;
    public static bool HraesvelgrGlows(BreathGlow glow) => glow is BreathGlow.Both or BreathGlow.Hraesvelgr;

    public PartyRole HraesvelgrStackTarget(int akhAfah) => (akhAfah == 0 ? HraesvelgrStackOnRegen : SecondHraesvelgrStackOnRegen) ? PartyRole.RegenHealer : PartyRole.ShieldHealer;
    public PartyRole NidhoggStackTarget(int akhAfah) => HraesvelgrStackTarget(akhAfah) == PartyRole.RegenHealer ? PartyRole.ShieldHealer : PartyRole.RegenHealer;

    public Vector3 NidhoggCauterizeStart => new(NidhoggCauterizesWest ? -DiveLaneOffset : DiveLaneOffset, 0f, -DiveStartDistance);
    public Vector3 HraesvelgrCauterizeStart => new(NidhoggCauterizesWest ? DiveLaneOffset : -DiveLaneOffset, 0f, -DiveStartDistance);

    public Vector3 NidhoggDiveStart => new(NidhoggDivesEast ? DiveLaneOffset : -DiveLaneOffset, 0f, NidhoggFromSouth ? DiveStartDistance : -DiveStartDistance);
    public Vector3 HraesvelgrDiveStart => new(HraesvelgrLane * DiveLaneOffset, 0f, HraesvelgrFromSouth ? DiveStartDistance : -DiveStartDistance);

    public (int X, int Z) ThirdSetCorner => (-SecondSetCorner.X, -SecondSetCorner.Z);

    // Set 0 sits on the centre, set 1 in the second corner, set 2 in the third.
    public IReadOnlyList<Vector3> OrbSet(int set)
    {
        var centre = set switch
        {
            1 => new Vector3(SecondSetCorner.X * CornerSetCentre, 0f, SecondSetCorner.Z * CornerSetCentre),
            2 => new Vector3(ThirdSetCorner.X * CornerSetCentre, 0f, ThirdSetCorner.Z * CornerSetCentre),
            _ => Vector3.Zero,
        };
        return Enumerable.Range(0, 3)
            .Select(row => centre + new Vector3((OrbColumns[set][row] - 1) * OrbSpacing, 0f, (row - 1) * OrbSpacing))
            .ToList();
    }
}
