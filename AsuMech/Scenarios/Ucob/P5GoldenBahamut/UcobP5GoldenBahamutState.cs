using System.Collections.Generic;
using System.Linq;
using AsuMech.Core.Game.Party;
using Scenario = AsuMech.Scenarios.Ucob.P5GoldenBahamut.UcobP5GoldenBahamutScenario;

namespace AsuMech.Scenarios.Ucob.P5GoldenBahamut;

public sealed class UcobP5GoldenBahamutState
{
    public const int ExaflareSets = 4;
    public const int MornAfahs = 5;

    private static readonly PartyRole[] NonTanks =
        [PartyRole.RegenHealer, PartyRole.ShieldHealer, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];

    public IReadOnlyList<UcobExaflarePattern> Exaflares { get; }
    // Morn Afah marks a non-tank (UNVERIFIED that a tank is never marked).
    public IReadOnlyList<PartyRole> MornAfahTargets { get; }
    public PartyRole FirstInvulnTank { get; }

    // Akh Morn goes to whoever holds him: shared, one tank's invuln, the other's, shared again. The
    // tank who invulns the third keeps him to the end.
    public IReadOnlyList<PartyRole> AkhMornTargets => [PartyRole.MainTank, FirstInvulnTank, OtherTank(FirstInvulnTank), OtherTank(FirstInvulnTank)];

    public static bool IsInvulnAkhMorn(int akhMorn) => akhMorn is 1 or 2;

    public UcobP5GoldenBahamutState(Rng rng, UcobP5GoldenBahamutStateOverrides? overrides = null)
    {
        overrides ??= new UcobP5GoldenBahamutStateOverrides();
        Exaflares = Enumerable.Range(0, ExaflareSets)
            .Select(i => Pinned(UcobExaflarePattern.Roll(rng, Scenario.ExaflareCasts[i]), overrides.ExaflareDirections[i], overrides.ExaflareLaneOrders?[i]))
            .ToList();
        var targets = Enumerable.Range(0, MornAfahs).Select(_ => NonTanks[rng.Next(NonTanks.Length)]).ToList();
        MornAfahTargets = overrides.MornAfahTargets ?? targets;
        FirstInvulnTank = overrides.FirstInvulnTank.IsTank() ? overrides.FirstInvulnTank : PartyRole.MainTank;
    }

    // Rolled whether pinned or not, so a pin leaves every later roll of the seed where it was.
    private static UcobExaflarePattern Pinned(UcobExaflarePattern rolled, Direction? direction, IReadOnlyList<float>? laneOrder)
        => direction is null && laneOrder is null ? rolled : new UcobExaflarePattern(rolled.BossCastAt, direction ?? rolled.Direction, laneOrder ?? rolled.LaneOrder);

    public static PartyRole OtherTank(PartyRole tank) => tank == PartyRole.MainTank ? PartyRole.OffTank : PartyRole.MainTank;
}
