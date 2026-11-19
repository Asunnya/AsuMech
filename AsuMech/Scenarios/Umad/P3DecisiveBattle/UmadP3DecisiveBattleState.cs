using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Umad.P3DecisiveBattle;

public sealed class UmadP3DecisiveBattleState
{
    public static readonly PartyRole[] Supports = [PartyRole.MainTank, PartyRole.OffTank, PartyRole.RegenHealer, PartyRole.ShieldHealer];
    public static readonly PartyRole[] Dps = [PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];

    // Intercardinals clockwise from north-east (0 NE, 1 SE, 2 SW, 3 NW).
    public int WindSpot { get; }
    public int FireSpot { get; }
    public int WaterSpot { get; }

    // Entropy (fire) and Dynamic Fluid (water) each go to one support and one DPS; one pair runs out early.
    public bool FireShort { get; }
    public (PartyRole Support, PartyRole Dps) Entropy { get; }
    public (PartyRole Support, PartyRole Dps) DynamicFluid { get; }

    public IReadOnlySet<PartyRole> Headwinds { get; }

    // Latitudinal hits the sides first, Longitudinal front and back first.
    public bool Latitudinal { get; }

    public UmadP3DecisiveBattleState(Rng rng, UmadP3DecisiveBattleStateOverrides? overrides = null)
    {
        overrides ??= new UmadP3DecisiveBattleStateOverrides();
        var windSpot = rng.Next(4);
        var fireClockwise = rng.NextBool();
        var fireShort = rng.NextBool();
        var latitudinal = rng.NextBool();
        var supports = rng.Shuffle(Supports).ToList();
        var dps = rng.Shuffle(Dps).ToList();
        var headwindCount = rng.NextInt(100) switch { < 39 => 4, < 71 => 5, < 96 => 3, _ => 2 };
        var headwinds = rng.Shuffle(Enum.GetValues<PartyRole>()).Take(headwindCount).ToHashSet();

        WindSpot = overrides.WindCrystalSpot is >= 0 and <= 3 ? overrides.WindCrystalSpot.Value : windSpot;
        fireClockwise = overrides.FireClockwiseOfWind ?? fireClockwise;
        FireSpot = (WindSpot + (fireClockwise ? 1 : 3)) % 4;
        WaterSpot = (WindSpot + (fireClockwise ? 3 : 1)) % 4;
        FireShort = overrides.FireShort ?? fireShort;
        Latitudinal = overrides.Latitudinal ?? latitudinal;
        Entropy = (Forced(supports, overrides.EntropyOn, overrides.DynamicFluidOn), Forced(dps, overrides.EntropyOn, overrides.DynamicFluidOn));
        supports.Remove(Entropy.Support);
        dps.Remove(Entropy.Dps);
        DynamicFluid = (Forced(supports, overrides.DynamicFluidOn, null), Forced(dps, overrides.DynamicFluidOn, null));
        Headwinds = headwinds;
    }

    // The other element's forced role is kept off this one.
    private static PartyRole Forced(List<PartyRole> group, PartyRole? forced, PartyRole? keepFree)
    {
        if (forced is { } f && group.Contains(f)) return f;
        return group.First(r => r != keepFree);
    }

    public static Vector2 Intercardinal(int spot)
    {
        var heading = MathF.PI * 3f / 4f - spot * MathF.PI / 2f;
        return new Vector2(MathF.Sin(heading), MathF.Cos(heading));
    }

    public bool HoldsEntropy(PartyRole role) => role == Entropy.Support || role == Entropy.Dps;

    public bool HoldsDynamicFluid(PartyRole role) => role == DynamicFluid.Support || role == DynamicFluid.Dps;

    public bool HoldsElement(bool fire, PartyRole role) => fire ? HoldsEntropy(role) : HoldsDynamicFluid(role);
}
