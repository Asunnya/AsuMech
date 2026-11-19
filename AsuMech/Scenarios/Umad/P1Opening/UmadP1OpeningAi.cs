using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Umad.P1Opening.UmadP1OpeningScenario;

namespace AsuMech.Scenarios.Umad.P1Opening;

public sealed class UmadP1OpeningAi : IScenarioAi<UmadP1OpeningState>
{
    public string Name => "Merry-Go-Round";

    private const float KnockbackDistance = 13f;

    private static readonly PartyRole[] LaserLineOrder =
    [
        PartyRole.ShieldHealer, PartyRole.RegenHealer, PartyRole.OffTank, PartyRole.MainTank,
        PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps,
    ];
    private static readonly float[] LaserLineX = [-18f, -12.5f, -7.5f, -2.5f, 2.5f, 7.5f, 12.5f, 18f];
    private const float LaserLineZ = 3f;

    private static readonly Vector2[] SpreadInQuadrant = [new(2f, 6f), new(8f, 6f), new(13.5f, 4f), new(6f, 0.8f)];
    private static readonly Vector2 StackInQuadrant = new(5.5f, 3.5f);

    private static readonly Dictionary<PartyRole, Vector2> FirstRockSpots = new()
    {
        [PartyRole.MainTank] = new(-15f, 2f), [PartyRole.OffTank] = new(0f, -6.5f),
        [PartyRole.RegenHealer] = new(-7f, 8f), [PartyRole.ShieldHealer] = new(7f, 8f),
        [PartyRole.MeleeDpsA] = new(-15f, 2f), [PartyRole.MeleeDpsB] = new(15f, 2f),
        [PartyRole.PhysRangedDps] = new(-7f, 8f), [PartyRole.CasterDps] = new(7f, 8f),
    };
    private static readonly Dictionary<PartyRole, Vector2> SecondRockSpots = new()
    {
        [PartyRole.MainTank] = new(-15f, -2f), [PartyRole.OffTank] = new(-8f, -4f),
        [PartyRole.RegenHealer] = new(8f, -4f), [PartyRole.ShieldHealer] = new(15f, -2f),
        [PartyRole.MeleeDpsA] = new(-15f, -2f), [PartyRole.MeleeDpsB] = new(-8f, -4f),
        [PartyRole.PhysRangedDps] = new(8f, -4f), [PartyRole.CasterDps] = new(15f, -2f),
    };

    private static readonly Vector2 TankSpotNorth = new(0f, -5f);
    private static readonly Vector2 RuinBaitNorth = new(0f, -5.5f);
    private static readonly Vector2 PartySouth = new(0f, 5f);

    private UmadP1OpeningState state = null!;
    private SimWorld world = null!;

    public void Run(UmadP1OpeningState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(role => OpeningSpot(role, PartyRole.MainTank, PartyRole.OffTank)), jitter: 0f);
        ai.Move(10.6f, () => ByRole(role => OpeningSpot(role, PartyRole.OffTank, PartyRole.MainTank)), jitter: 0f);
        ai.Move(22.0f, () => ByRole(WaitOnOwnSideForTheTells), jitter: 0f);
        ai.Move(FirstMysteryMagicAt + 0.1f, () => ByRole(FirstSetSpotBeforeTheKnockback), jitter: 0f, arrivalTime: PulseWaveAt - 0.4f);
        ai.Move(PulseWaveAt + 1.3f, () => ByRole(FirstSetSpot), jitter: 0f);
        ai.Move(FireAt + 0.3f, () => ByRole(LaserLineSpot), jitter: 0f, arrivalTime: LaserAt - 0.4f);
        ai.Move(TowersAt + 0.1f, () => ByRole(TowerOrStepOutOfIt), jitter: 0f, arrivalTime: TowersAt + 2.6f);
        ai.Move(38.1f, () => ByRole(role => ConfettiAcrossTheMiddle(role, 0)), jitter: 0f, arrivalTime: FirstConfettiAt - 0.4f);
        ai.Move(43.05f, () => ByRole(NearestSpotClearOfIceAndThunder), jitter: 0f, arrivalTime: SecondMysteryResolveAt - 0.3f);
        ai.Move(SecondMysteryResolveAt + 0.4f, () => ByRole(role => OpeningSpot(role, PartyRole.OffTank, PartyRole.MainTank)), jitter: 0f);

        ai.Move(ThirdIceAt + 0.1f, () => ByRole(role => QuadStackNorthClearOfIce(role)), jitter: 0f, arrivalTime: GravitasAt[0] - 0.6f);
        ai.Move(GravitasAt[0] + 0.15f, () => ByRole(role => RocksSpreadGravityMiddle(role, 0)), jitter: 0f, arrivalTime: VitrophyreAt[0] - 0.4f);
        ai.Move(WillWindUpAt[0] + 0.2f, () => ByRole(role => DodgeWill(role, 0, 4f)), jitter: 0f, arrivalTime: WillAt[0] - 0.4f);
        ai.Move(WillAt[0] + 0.2f, () => ByRole(role => Cluster(new Vector2(0f, 12f), role)), jitter: 0f, arrivalTime: GravitasAt[1] - 0.5f);
        ai.Move(GravitasAt[1] + 0.15f, () => ByRole(role => RocksSpreadGravityMiddle(role, 1)), jitter: 0f, arrivalTime: VitrophyreAt[1] - 0.4f);
        ai.Move(VitrophyreAt[1] + 0.2f, () => ByRole(role => DodgeWill(role, 1, 2f)), jitter: 0f, arrivalTime: WillAt[1] - 0.4f);
        ai.Move(WillAt[1] + 0.2f, () => ByRole(ConfettiThroughThePuddles), jitter: 0f, arrivalTime: PuddlesArmAt - 0.3f);
        ai.Move(SecondConfettiAt + 1.5f, () => ByRole(SoakThePuddlesYouLandedOn), jitter: 0f);
        ai.Move(UnpoppedPuddlesAt + 0.2f, () => ByRole(role => OpeningSpot(role, PartyRole.MainTank, PartyRole.OffTank)), jitter: 0f);
    }

    private static int Side(PartyRole role) => role.IsDps() ? 1 : -1;

    private static int SlotInGroup(PartyRole role) => (int)role % 4;

    private static Vector2 Cluster(Vector2 center, PartyRole role)
    {
        var angle = (int)role * MathF.PI / 4f;
        return center + new Vector2(MathF.Sin(angle), MathF.Cos(angle)) * 0.6f;
    }

    private static Vector2 OpeningSpot(PartyRole role, PartyRole tank, PartyRole otherTank)
    {
        if (role == tank) return TankSpotNorth;
        if (role == otherTank) return new Vector2(-4f, 1f);
        return Cluster(PartySouth, role);
    }

    private Vector2 WaitOnOwnSideForTheTells(PartyRole role)
        => new(Side(role) * 5f, state.IsTethered(role) ? -15f : 0f);

    private Vector2 FirstSetSpot(PartyRole role)
    {
        var side = Side(role);
        var north = state.FirstIce.RealNwSe ? -side : side;
        var unit = new Vector2(side, north);
        if (state.FireIsStack) return StackInQuadrant * unit + Cluster(Vector2.Zero, role) * 0.5f;
        return SpreadInQuadrant[SlotInGroup(role)] * unit;
    }

    private Vector2 FirstSetSpotBeforeTheKnockback(PartyRole role)
    {
        var final = FirstSetSpot(role);
        if (!state.IsTethered(role)) return final;
        var source = new Vector2(PulseWaveSource.X, PulseWaveSource.Z);
        return final - Vector2.Normalize(final - source) * KnockbackDistance;
    }

    private static Vector2 LaserLineSpot(PartyRole role)
        => new(LaserLineX[Array.IndexOf(LaserLineOrder, role)], LaserLineZ);

    private Vector2 TowerOrStepOutOfIt(PartyRole role)
    {
        var line = LaserLineSpot(role);
        if (state.IsLasered(role)) return line - new Vector2(0f, 6.5f);
        var towers = LaserLineOrder.Where(state.IsLasered).Select(LaserLineSpot).ToList();
        var soakers = LaserLineOrder.Where(r => !state.IsLasered(r)).ToList();
        return towers[soakers.IndexOf(role)];
    }

    private Vector2 ConfettiAcrossTheMiddle(PartyRole role, int wave)
    {
        var (support, dps) = state.Confetti[wave];
        var side = Side(role);
        if (role == support || role == dps) return new Vector2(side * 8.5f, 0f);
        return new Vector2(side * 6f, (SlotInGroup(role) - 1.5f) * 0.5f);
    }

    private Vector2 NearestSpotClearOfIceAndThunder(PartyRole role)
    {
        var from = world.Party.Get(role) is { } member ? new Vector2(member.Position.X, member.Position.Z) : Vector2.Zero;
        var best = from;
        var bestDistance = float.MaxValue;
        for (var x = -16f; x <= 16f; x += 0.5f)
            for (var z = -16f; z <= 16f; z += 0.5f)
            {
                var spot = new Vector2(x, z);
                if (spot.Length() is > 16.5f or < 3f) continue;
                if (InIce(state.SecondIce, spot, 0.2f) || InThunder(state, spot, 1.5f)) continue;
                var distance = Vector2.Distance(spot, from);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = spot;
            }
        return best;
    }

    private float NorthStackX => state.ThirdIce.RealNwSe ? 3.5f : -3.5f;

    private Vector2 QuadStackNorthClearOfIce(PartyRole role) => Cluster(new Vector2(NorthStackX, -11f), role);

    private Vector2 RocksSpreadGravityMiddle(PartyRole role, int round)
    {
        if (round == 0 && role == PartyRole.OffTank) return RuinBaitNorth;
        if (!state.TakesGravitas(role, round)) return (round == 0 ? FirstRockSpots : SecondRockSpots)[role];
        return Cluster(new Vector2(0f, round == 0 ? 3.5f : 3f), role);
    }

    private Vector2 DodgeWill(PartyRole role, int round, float z)
    {
        var away = state.WillEast[round] ? -1f : 1f;
        if (round == 0 && role == PartyRole.OffTank) return new Vector2(away * 4f, RuinBaitNorth.Y);
        return Cluster(new Vector2(away * 6f, z), role);
    }

    private Vector2 ConfettiThroughThePuddles(PartyRole role)
    {
        var (support, dps) = state.Confetti[1];
        if (role == support) return new Vector2(0f, -6f);
        if (role == dps) return new Vector2(NorthStackX, 6f);
        var inColumn = SlotInGroup(role) * 0.4f;
        return role.IsDps() ? new Vector2(NorthStackX, 2.6f + inColumn) : new Vector2(0f, -2.6f - inColumn);
    }

    private Vector2 SoakThePuddlesYouLandedOn(PartyRole role)
        => role.IsDps() ? Cluster(new Vector2(NorthStackX, -10.5f), role) : Cluster(new Vector2(0f, 10.5f), role);

    private static IAiMove ByRole(Func<PartyRole, Vector2> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(r => (Vector2?)at(r)).ToArray()).NaturalOrder();
}
