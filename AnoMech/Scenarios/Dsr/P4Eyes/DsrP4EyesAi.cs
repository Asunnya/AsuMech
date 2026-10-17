using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Dsr.DsrConstants;

namespace AnoMech.Scenarios.Dsr.P4Eyes;

public sealed class DsrP4EyesAi : IScenarioAi<DsrP4EyesState>
{
    public string Name => "NA PF (snake swaps)";

    public enum Corner { NorthWest, SouthWest, SouthEast, NorthEast }

    private const float Step = 0.5f;
    private static readonly Vector2 AlphinaudStack = new(0f, 8.8f);
    private static readonly Vector2 Estinien = Vector2.Zero;
    private static readonly Vector2 BlueEye = new(-10f, 0f);
    private static readonly PartyRole[] Swappers = [PartyRole.MainTank, PartyRole.OffTank, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB];
    private static readonly Corner[] GroupOneSnake = [Corner.NorthWest, Corner.SouthWest, Corner.SouthEast, Corner.NorthEast];
    private static readonly Corner[] GroupTwoSnake = [Corner.NorthEast, Corner.SouthEast, Corner.SouthWest, Corner.NorthWest];

    private static readonly Dictionary<PartyRole, Vector2> EyeSpread = new()
    {
        [PartyRole.MainTank] = new(6.5f, 2.3f),
        [PartyRole.OffTank] = new(6.5f, -1.7f),
        [PartyRole.RegenHealer] = new(5f, -5.6f),
        [PartyRole.ShieldHealer] = new(5f, 6.6f),
        [PartyRole.MeleeDpsA] = new(-5.9f, -1.8f),
        [PartyRole.MeleeDpsB] = new(-5.9f, 2.3f),
        [PartyRole.PhysRangedDps] = new(-3.3f, -5.6f),
        [PartyRole.CasterDps] = new(-4.2f, 6.6f),
    };

    private static readonly Dictionary<PartyRole, Vector2> GildedOrbSpot = new()
    {
        [PartyRole.MainTank] = new(14.4f, -1f),
        [PartyRole.OffTank] = new(14.4f, 1f),
        [PartyRole.MeleeDpsA] = new(-14.4f, -1f),
        [PartyRole.MeleeDpsB] = new(-14.4f, 1f),
    };

    private static readonly Dictionary<PartyRole, PartyRole> PassPartner = new()
    {
        [PartyRole.MainTank] = PartyRole.RegenHealer,
        [PartyRole.OffTank] = PartyRole.ShieldHealer,
        [PartyRole.MeleeDpsA] = PartyRole.PhysRangedDps,
        [PartyRole.MeleeDpsB] = PartyRole.CasterDps,
    };

    private static readonly Dictionary<PartyRole, Vector2> AzureOrb = new()
    {
        [PartyRole.RegenHealer] = new(10f, -7f),
        [PartyRole.ShieldHealer] = new(10f, 7f),
        [PartyRole.PhysRangedDps] = new(-10f, -7f),
        [PartyRole.CasterDps] = new(-10f, 7f),
    };

    private static readonly Dictionary<PartyRole, Corner> HomeCorner = new()
    {
        [PartyRole.RegenHealer] = Corner.NorthEast,
        [PartyRole.ShieldHealer] = Corner.SouthEast,
        [PartyRole.PhysRangedDps] = Corner.NorthWest,
        [PartyRole.CasterDps] = Corner.SouthWest,
    };

    private static readonly Dictionary<PartyRole, Vector2> BlueEyeHuddle = new()
    {
        [PartyRole.MainTank] = BlueEye + new Vector2(-1f, -0.6f),
        [PartyRole.OffTank] = BlueEye + new Vector2(1f, -0.6f),
        [PartyRole.MeleeDpsA] = BlueEye + new Vector2(-1f, 0.6f),
        [PartyRole.MeleeDpsB] = BlueEye + new Vector2(1f, 0.6f),
        [PartyRole.RegenHealer] = BlueEye + new Vector2(0.6f, -1.2f),
        [PartyRole.ShieldHealer] = BlueEye + new Vector2(0.6f, 1.2f),
        [PartyRole.PhysRangedDps] = BlueEye + new Vector2(-0.6f, -1.2f),
        [PartyRole.CasterDps] = BlueEye + new Vector2(-0.6f, 1.2f),
    };

    private const float AzureWaitDistance = 3.1f;
    private const float AzurePopDistance = 2.6f;
    private const float PassOffset = 1f;

    private DsrP4EyesState state = null!;
    private SimWorld world = null!;
    private readonly Dictionary<PartyRole, Corner>[] swapPlans = [[], [], [], []];
    private readonly Dictionary<PartyRole, Corner> firstDiveCorners = [];

    public void Run(DsrP4EyesState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        foreach (var plan in swapPlans) plan.Clear();
        firstDiveCorners.Clear();
        var ai = new AiManager(world);

        ai.Move(0.3f, () => AiMove.Create(Stacked(AlphinaudStack)).NaturalOrder());
        ai.Move(18.0f, () => AiMove.Create(SupportsToRedEyeDpsToBlueEye()).NaturalOrder(), jitter: 0f);
        Every(ai, 35.5f, 40.5f, TradeWrongColoursOnEstinienThenPreposition);
        Every(ai, 41.2f, 46.5f, MeleeHandRedToRangedAfterGildedPop);
        Every(ai, 47.0f, 58.5f, RangedToBoxCornersAfterAzurePop);
        for (var dive = 0; dive < DsrP4EyesState.Dives - 1; dive++)
        {
            var d = dive;
            world.Events.Add(DsrP4EyesScenario.DiveTimes[d] + 0.2f, () => PlanSwaps(d));
            Every(ai, DsrP4EyesScenario.DiveTimes[d] + 0.3f, DsrP4EyesScenario.DiveTimes[d] + 4.8f, () => SwapIntoDivedCorners(d));
        }
        world.Events.Add(DsrP4EyesScenario.DiveTimes[0] + 0.2f, RememberFirstDiveCorners);
    }

    private static void Every(AiManager ai, float from, float to, Func<Vector2?[]> spots)
    {
        for (var t = from; t <= to + 0.001f; t += Step)
            ai.Move(t, () => AiMove.Create(spots()).NaturalOrder(), jitter: 0f, sprint: true);
    }

    private static Vector2?[] SupportsToRedEyeDpsToBlueEye()
    {
        var spots = new Vector2?[8];
        foreach (var (role, spot) in EyeSpread) spots[(int)role] = spot;
        return spots;
    }

    private static bool WantsRed(PartyRole role) => Swappers.Contains(role);

    private bool HasWrongColour(PartyRole role) => state.HoldsRed(role) != WantsRed(role);

    private Vector2?[] TradeWrongColoursOnEstinienThenPreposition()
    {
        var spots = new Vector2?[8];
        foreach (var role in Enum.GetValues<PartyRole>())
            spots[(int)role] = HasWrongColour(role) ? Estinien : WantsRed(role) ? GildedOrbSpot[role] : AzureWait(role);
        return spots;
    }

    private Vector2?[] MeleeHandRedToRangedAfterGildedPop()
    {
        var spots = new Vector2?[8];
        foreach (var (melee, ranged) in PassPartner)
        {
            if (!state.HoldsRed(melee)) spots[(int)melee] = BlueEyeHuddle[melee];
            else if (Popped(melee)) spots[(int)melee] = PassSpot(ranged);
            spots[(int)ranged] = state.HoldsRed(ranged) ? AzurePop(ranged) : AzureWait(ranged);
        }
        return spots;
    }

    private Vector2?[] RangedToBoxCornersAfterAzurePop()
    {
        var spots = new Vector2?[8];
        foreach (var (melee, ranged) in PassPartner)
        {
            spots[(int)melee] = BlueEyeHuddle[melee];
            if (!state.HoldsRed(ranged)) spots[(int)ranged] = AzureWait(ranged);
            else spots[(int)ranged] = Popped(ranged) ? CornerSpot(HomeCorner[ranged]) : AzurePop(ranged);
        }
        return spots;
    }

    private bool Popped(PartyRole role) => world.Party.Get(role) is { } member && member.HasStatus(StatusId.FireResistanceDownII);

    private void RememberFirstDiveCorners()
    {
        foreach (var role in state.DiveTargets[0])
            if (world.Party.Get(role) is { } member)
                firstDiveCorners[role] = NearestCorner(member.Position);
    }

    private void PlanSwaps(int dive)
    {
        var plan = swapPlans[dive];
        plan.Clear();
        var dived = state.DiveTargets[dive]
            .Select(r => world.Party.Get(r))
            .Where(m => m != null)
            .Select(m => NearestCorner(m!.Position))
            .ToList();
        if (dived.Count == 0) return;
        var (groupOne, groupTwo) = SwappersFor(dive);
        if (groupOne is { } one) plan[one] = GroupOneSnake.First(dived.Contains);
        if (groupTwo is { } two && dived.Count > 1)
            plan[two] = GroupTwoSnake.First(c => dived.Contains(c) && !plan.ContainsValue(c));
    }

    private (PartyRole?, PartyRole?) SwappersFor(int dive)
    {
        switch (dive)
        {
            case 0: return (PartyRole.MainTank, PartyRole.OffTank);
            case 1: return (PartyRole.MeleeDpsA, PartyRole.MeleeDpsB);
            default:
            {
                var firsts = firstDiveCorners.OrderBy(kv => Array.IndexOf(GroupOneSnake, kv.Value)).Select(kv => (PartyRole?)kv.Key).ToList();
                return (firsts.ElementAtOrDefault(0), firsts.ElementAtOrDefault(1));
            }
        }
    }

    private Vector2?[] SwapIntoDivedCorners(int dive)
    {
        var spots = new Vector2?[8];
        foreach (var (role, corner) in swapPlans[dive])
            spots[(int)role] = CornerSpot(corner);
        foreach (var role in state.DiveTargets[dive].Where(r => !state.HoldsRed(r)))
            spots[(int)role] = BlueEyeHuddle[role];
        return spots;
    }

    public static Vector2 CornerSpot(Corner corner) => corner switch
    {
        Corner.NorthWest => new Vector2(-15f, -5f),
        Corner.SouthWest => new Vector2(-15f, 5f),
        Corner.SouthEast => new Vector2(-5f, 5f),
        _ => new Vector2(-5f, -5f),
    };

    private static Corner NearestCorner(Vector3 at) =>
        Enum.GetValues<Corner>().OrderBy(c => Vector2.Distance(CornerSpot(c), new Vector2(at.X, at.Z))).First();

    private static Vector2 TowardsCentreLine(Vector2 orb, float distance) => orb - new Vector2(0f, MathF.Sign(orb.Y) * distance);

    private static Vector2 AzureWait(PartyRole role) => TowardsCentreLine(AzureOrb[role], AzureWaitDistance);

    private static Vector2 AzurePop(PartyRole role) => TowardsCentreLine(AzureOrb[role], AzurePopDistance);

    private static Vector2 PassSpot(PartyRole ranged) => TowardsCentreLine(AzureOrb[ranged], AzureWaitDistance + PassOffset);

    private static Vector2?[] Stacked(Vector2 centre) =>
        Enumerable.Range(0, 8).Select(i => (Vector2?)(centre + new Vector2(0.5f * (i % 3 - 1), 0.5f * (i / 3 - 1)))).ToArray();
}
