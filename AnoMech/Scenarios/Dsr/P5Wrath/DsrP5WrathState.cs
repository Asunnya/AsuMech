using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;
using static AnoMech.Scenarios.Dsr.DsrConstants;

namespace AnoMech.Scenarios.Dsr.P5Wrath;

public sealed class DsrP5WrathState
{
    public const float KnightRadius = 23f;
    public const float DragoonOffset = 30f;
    public const float CasterRadius = 9f;

    public float DragonBearing { get; }
    // Ser Charibert, the mage, and Ser Grinnaux, the warrior, face each other across the dragon's axis.
    public bool MageOnDragonSide { get; }
    public IReadOnlyList<PartyRole> Tethered { get; }
    public PartyRole Defamation { get; }
    public PartyRole Divebomb { get; }
    public IReadOnlyList<PartyRole> Lightning { get; }
    public PartyRole LiquidHeaven { get; }
    // Simplified: always a tethered player or the defamation, never the divebomb.
    public PartyRole Eruptions { get; }

    public DsrP5WrathState(Rng rng, DsrP5WrathStateOverrides? overrides = null)
    {
        overrides ??= new DsrP5WrathStateOverrides();
        DragonBearing = overrides.DragonBearing ?? 90f * rng.Next(4);
        MageOnDragonSide = overrides.MageOnDragonSide ?? rng.NextBool();
        var order = rng.Shuffle(Enum.GetValues<PartyRole>()).ToList();
        Tethered = overrides.Tethered ?? order.Take(2).ToList();
        Defamation = overrides.Defamation ?? order.Except(Tethered).First();
        Divebomb = overrides.Divebomb ?? order.Except(Tethered).Where(r => r != Defamation).First();
        var lightning = rng.Shuffle(Enum.GetValues<PartyRole>()).Take(2).ToList();
        Lightning = overrides.Lightning ?? lightning;
        var plain = Plain.ToList();
        var liquid = plain[rng.Next(plain.Count)];
        LiquidHeaven = overrides.LiquidHeaven ?? liquid;
        var marked = Marked.ToList();
        var eruptions = marked[rng.Next(marked.Count)];
        Eruptions = overrides.Eruptions ?? eruptions;
    }

    // The tethered players and the defamation.
    public IEnumerable<PartyRole> Marked => Tethered.Append(Defamation).OrderBy(r => r);

    // Everyone with neither a tether, the defamation nor the divebomb.
    public IEnumerable<PartyRole> Plain => Enum.GetValues<PartyRole>().Where(r => !Tethered.Contains(r) && r != Defamation && r != Divebomb);

    public float WarriorBearing => MageOnDragonSide ? DragonBearing + 180f : DragonBearing;
    public float MageBearing => WarriorBearing + 180f;

    public Vector3 DragonSpot => AtBearing(DragonBearing, KnightRadius);
    // Ignasse sits clockwise of the dragon, Vellguine counter-clockwise.
    public Vector3 DragoonSpot(int dragoon) => AtBearing(DragonBearing + (dragoon == 0 ? DragoonOffset : -DragoonOffset), KnightRadius);
    public Vector3 WarriorSpot => AtBearing(WarriorBearing, CasterRadius);
    public Vector3 MageSpot => AtBearing(MageBearing, CasterRadius);
}
