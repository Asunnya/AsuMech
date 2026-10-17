using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Dsr.P1Knights;
using static AnoMech.Scenarios.Dsr.DsrConstants;

namespace AnoMech.Scenarios.Dsr.P5Death;

public sealed class DsrP5DeathState
{
    public const float CasterRadius = 9f;
    public const float OutsideRadius = 23f;
    public const float ThordanRadius = 25f;
    public const float EyeRadius = 40f;
    // Bearings from the warrior of the static dives.
    public const float SpearBearing = 60f;
    public const float TwisterDragonBearing = 120f;
    public const float CauterizeBearing = 0f;
    public const float WingsBearing = 270f;
    public const float HeavensflameBearing = 90f;
    public const float MeteorKnightBearing = 180f;

    public float WarriorBearing { get; }
    public IReadOnlyCollection<PartyRole> Dooms { get; }
    public float ThordanBearing { get; }
    public float EyeBearing { get; }
    public bool FirstDoomSquare { get; }
    public int? PlayerCongaSlot { get; }
    // Order the non-dooms without a circle take the crosses and the leftover square and triangle.
    public IReadOnlyList<int> NonDoomOrder { get; }

    // Handed out by the scenario as the markers land.
    public Dictionary<PartyRole, ChainSymbol> Symbols { get; } = [];
    // Where the non-dooms dropped their cleansing puddles.
    public List<Vector3> Puddles { get; } = [];

    public DsrP5DeathState(Rng rng, DsrP5DeathStateOverrides? overrides = null)
    {
        overrides ??= new DsrP5DeathStateOverrides();
        WarriorBearing = overrides.WarriorBearing ?? 90f * rng.Next(4);
        List<PartyRole> dooms;
        do dooms = rng.Shuffle(Enum.GetValues<PartyRole>()).Take(4).ToList();
        while (dooms.All(InFirstLightParty) || !dooms.Any(InFirstLightParty));
        Dooms = overrides.Dooms ?? dooms;
        var thordan = 45f * rng.Next(8);
        ThordanBearing = overrides.ThordanBearing ?? thordan;
        var eye = 45f * rng.Next(8);
        EyeBearing = overrides.EyeBearing ?? eye;
        var firstSquare = rng.NextBool();
        FirstDoomSquare = overrides.FirstDoomSquare ?? firstSquare;
        NonDoomOrder = rng.Shuffle(new[] { 0, 1, 2, 3 }).ToList();
        PlayerCongaSlot = overrides.PlayerCongaSlot;
    }

    // UNVERIFIED: the four dooms never all land in one light party.
    public static bool InFirstLightParty(PartyRole role) =>
        role is PartyRole.MainTank or PartyRole.RegenHealer or PartyRole.MeleeDpsA or PartyRole.PhysRangedDps;

    public bool IsDoomed(PartyRole role) => Dooms.Contains(role);

    public Vector3 Outside(float bearingFromWarrior, float radius) => AtBearing(WarriorBearing + bearingFromWarrior, radius);
    public Vector3 WarriorSpot => Outside(0f, CasterRadius);
    public Vector3 ThordanSpot => AtBearing(ThordanBearing, ThordanRadius);
    public Vector3 EyeSpot => AtBearing(EyeBearing, EyeRadius);
    public byte EyeSlot => (byte)(EyeBearing / 45f);

    public IEnumerable<(PartyRole A, PartyRole B, ChainSymbol Symbol)> ChainPairs() =>
        Symbols.GroupBy(kv => kv.Value)
            .Where(g => g.Count() == 2)
            .Select(g => (g.First().Key, g.Last().Key, g.Key));

    // The two farthest from the centre take the circles; the other dooms a square and a triangle,
    // the other non-dooms the crosses and the rest.
    public void HandOutSymbols(IReadOnlyDictionary<PartyRole, float> distanceFromCentre)
    {
        Symbols.Clear();
        var byDistance = distanceFromCentre.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToList();
        foreach (var role in byDistance.Take(2)) Symbols[role] = ChainSymbol.Circle;
        var rest = byDistance.Skip(2).OrderBy(r => r).ToList();
        var pool = new List<ChainSymbol> { ChainSymbol.Square, ChainSymbol.Triangle, ChainSymbol.Square, ChainSymbol.Triangle, ChainSymbol.Cross, ChainSymbol.Cross };
        var doomOrder = FirstDoomSquare ? new[] { ChainSymbol.Square, ChainSymbol.Triangle } : [ChainSymbol.Triangle, ChainSymbol.Square];
        var doomIndex = 0;
        foreach (var role in rest.Where(IsDoomed))
        {
            var symbol = doomIndex < doomOrder.Length ? doomOrder[doomIndex++] : pool[0];
            pool.Remove(symbol);
            Symbols[role] = symbol;
        }
        var nonDooms = rest.Where(r => !IsDoomed(r)).ToList();
        var shuffled = NonDoomOrder.Where(i => i < nonDooms.Count).Select(i => nonDooms[i]).ToList();
        foreach (var role in shuffled)
        {
            var symbol = pool.Contains(ChainSymbol.Cross) ? ChainSymbol.Cross : pool[0];
            pool.Remove(symbol);
            Symbols[role] = symbol;
        }
    }
}
