using System.Collections.Generic;
using System.Linq;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Umad.P1Opening;

// Blizzard III Blowout lights two opposite quadrants: NW+SE or NE+SW. A lie shows the other pair.
public sealed record IceSet(bool RealNwSe, bool IsLie)
{
    public bool ShownNwSe => RealNwSe != IsLie;
}

public sealed class UmadP1OpeningState
{
    public static readonly PartyRole[] Supports = [PartyRole.MainTank, PartyRole.OffTank, PartyRole.RegenHealer, PartyRole.ShieldHealer];
    public static readonly PartyRole[] Dps = [PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];

    public const int GravenRounds = 2;

    // The first Graven Image tethers one whole role group.
    public bool TetheredAreDps { get; }

    public bool FireIsStack { get; }
    public bool FireIsLie { get; }
    public bool FireShowsStackIcon => FireIsStack != FireIsLie;
    public PartyRole FireStackSupport { get; }
    public PartyRole FireStackDps { get; }

    public IceSet FirstIce { get; }
    public IceSet SecondIce { get; }
    public IceSet ThirdIce { get; }

    // Two of the four Thrumming Thunder III lanes are real (UmadP1TeleTrouncingScenario.ThunderAnchors).
    public int ThunderRealOffset { get; }
    public bool ThunderOrientationFlipped { get; }
    public bool ThunderIsLie { get; }
    public float ThunderOrientation => ThunderOrientationFlipped ? -1f : 1f;

    // Wave Cannon picks two supports and two DPS.
    public IReadOnlyList<PartyRole> LaserTargets { get; }

    // Double-trouble Trap sits on one support and one DPS and passes to a new pair as it goes off.
    public IReadOnlyList<(PartyRole Support, PartyRole Dps)> Confetti { get; }

    // Each round of the second Graven Image, one group is tethered to gravity and the other to rock.
    public IReadOnlyList<bool> GravitasOnDps { get; }

    // Each round's half-room Will: east (Intemperate Will) or west (Gravitational Wave).
    public IReadOnlyList<bool> WillEast { get; }

    public UmadP1OpeningState(Rng rng, UmadP1OpeningStateOverrides? overrides = null)
    {
        overrides ??= new UmadP1OpeningStateOverrides();
        TetheredAreDps = Roll(rng, overrides.TetheredAreDps);
        FireIsStack = Roll(rng, overrides.FireIsStack);
        FireIsLie = Roll(rng, overrides.FireIsLie);
        FireStackSupport = Pick(rng, Supports, overrides.FireStackOn);
        FireStackDps = Pick(rng, Dps, overrides.FireStackOn);
        FirstIce = new IceSet(rng.NextBool(), Roll(rng, overrides.FirstIceIsLie));
        SecondIce = new IceSet(rng.NextBool(), Roll(rng, overrides.SecondIceIsLie));
        ThirdIce = new IceSet(rng.NextBool(), Roll(rng, overrides.ThirdIceIsLie));
        ThunderRealOffset = rng.Next(2);
        ThunderOrientationFlipped = rng.NextBool();
        ThunderIsLie = Roll(rng, overrides.ThunderIsLie);
        LaserTargets = [.. PickTwo(rng, Supports, overrides.LaserOn), .. PickTwo(rng, Dps, overrides.LaserOn)];
        Confetti =
        [
            (Pick(rng, Supports, overrides.FirstConfettiOn), Pick(rng, Dps, overrides.FirstConfettiOn)),
            (Pick(rng, Supports, overrides.SecondConfettiOn), Pick(rng, Dps, overrides.SecondConfettiOn)),
            (rng.NextObj(Supports), rng.NextObj(Dps)),
        ];
        GravitasOnDps = [Roll(rng, overrides.FirstGravitasOnDps), Roll(rng, overrides.SecondGravitasOnDps)];
        WillEast = [Roll(rng, overrides.FirstWillEast), Roll(rng, overrides.SecondWillEast)];
    }

    // Rolled whether or not it is pinned, so pinning one roll leaves the others as the seed had them.
    private static bool Roll(Rng rng, bool? pinned)
    {
        var rolled = rng.NextBool();
        return pinned ?? rolled;
    }

    public bool IsTethered(PartyRole role) => role.IsDps() == TetheredAreDps;

    public bool TakesGravitas(PartyRole role, int round) => role.IsDps() == GravitasOnDps[round];

    public bool IsLasered(PartyRole role) => LaserTargets.Contains(role);

    public bool IsFireStackHolder(PartyRole role) => FireIsStack && (role == FireStackSupport || role == FireStackDps);

    private static PartyRole Pick(Rng rng, PartyRole[] group, PartyRole? forced)
    {
        var rolled = rng.NextObj(group);
        return forced is { } f && group.Contains(f) ? f : rolled;
    }

    private static IEnumerable<PartyRole> PickTwo(Rng rng, PartyRole[] group, PartyRole? forced)
    {
        var shuffled = rng.Shuffle(group).ToList();
        if (forced is { } f && group.Contains(f) && !shuffled.Take(2).Contains(f))
            shuffled[0] = f;
        return shuffled.Take(2);
    }
}
