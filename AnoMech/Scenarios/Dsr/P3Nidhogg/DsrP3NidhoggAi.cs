using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Dsr.DsrConstants;

namespace AnoMech.Scenarios.Dsr.P3Nidhogg;

public sealed class DsrP3NidhoggAi : IScenarioAi<DsrP3NidhoggState>
{
    public string Name => "NAUR (PF)";

    private const float FacingWest = -MathF.PI / 2f;
    private const float OddDiveRadius = 8f;
    private const float EvenDiveRadius = 10f;
    private const float ThirdsClaimRadius = 10f;
    private const float InsideWheelRadius = 3.5f;
    private const float OutsideWheelRadius = 10f;
    private const float TowerEdge = 3.5f;
    private const float BaitBeyondTower = 4f;
    private const float FourTowerBaitRadius = 16f;
    private const float FourTowerDodgeRadius = 15f;
    private const float TankBusterOffTankRadius = 9f;
    private const float TetherInterceptShare = 0.5f;
    private static readonly Vector2 MainTankSpot = new(0f, -6f);
    private static readonly Vector2 NorthStack = new(0f, -8f);
    private static readonly Vector2 SouthStack = new(0f, 6f);
    private static readonly Vector2 NorthWestHold = new(-6f, -6f);
    private static readonly float[] OddDiveBearings = [90f, 180f, 270f];
    private static readonly float[] EvenDiveBearings = [45f, 315f];
    private static readonly (PartyRole Anchor, PartyRole Partner)[] FourTowerPairs =
    [
        (PartyRole.CasterDps, PartyRole.OffTank),
        (PartyRole.ShieldHealer, PartyRole.MeleeDpsA),
        (PartyRole.PhysRangedDps, PartyRole.MainTank),
        (PartyRole.RegenHealer, PartyRole.MeleeDpsB),
    ];

    private DsrP3NidhoggState state = null!;
    private SimWorld world = null!;

    public void Run(DsrP3NidhoggState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => AiMove.Create(MainTankNorthPartySouth()).NaturalOrder());

        ai.Move(23.5f, () => AiMove.Create(ClaimDiveSpotsByNumber()).NaturalOrder(), jitter: 0f);
        ai.Move(30.5f, () => AiMove.Create(DiveSpotsAndNorthStack(1)).NaturalOrder(), jitter: 0f);
        world.Events.Add(36.7f, () => FaceArrowsWest(1));
        ai.Move(37.5f, () => AiMove.Create(WaitOnTowerBearingsForFirstWheel(0, 1)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(41.4f, () => AiMove.Create(SoakDiveTowers(1, 0)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(44.2f, () => AiMove.Create(BaitGeirskoguls(1)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(44.2f, () => AiMove.Create(EvenDiveSpots()).NaturalOrder(), jitter: 0f, sprint: true);
        world.Events.Add(46.7f, () => FaceArrowsWest(2));
        ai.Move(47.45f, () => AiMove.Create(BaitersToNorthStack(1)).NaturalOrder(), jitter: 0f, sprint: true);

        ai.Move(47.5f, () => AiMove.Create(EvenDiversToNorthStack()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(47.5f, () => AiMove.Create(SoakDiveTowers(2, null)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(53.95f, () => AiMove.Create(BaitGeirskoguls(2)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(53.95f, () => AiMove.Create(OddDiveSpots(3)).NaturalOrder(), jitter: 0f, sprint: true);
        world.Events.Add(57.7f, () => FaceArrowsWest(3));
        ai.Move(56.6f, () => AiMove.Create(BaitersToNorthStack(2)).NaturalOrder(), jitter: 0f, sprint: true);

        ai.Move(59.1f, () => AiMove.Create(WaitOnTowerBearingsForFirstWheel(1, 3)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(62.5f, () => AiMove.Create(SoakDiveTowers(3, 1)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(65.6f, () => AiMove.Create(BaitGeirskoguls(3)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(67.6f, () => AiMove.Create(EveryoneButMainTankNorthWest()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(72.5f, () => AiMove.Create(MainTankNorthPartySouth()).NaturalOrder(), sprint: true);

        ai.Move(74.3f, () => AiMove.Create(MainTankStepsOutOfDrachenlance()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(77.8f, () => AiMove.Create(SoakFourTowers()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(84.2f, () => AiMove.Create(BaitFourTowerGeirskoguls()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(86.8f, () => AiMove.Create(DodgeToCardinalsAndInterceptTethers()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(89.2f, () => AiMove.Create(TanksApartForSoulTethers()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(91.3f, () => AiMove.Create(MainTankNorthPartySouth()).NaturalOrder(), sprint: true);
        ai.Move(110.5f, () => AiMove.Create(EveryoneNorthOfTheSouthDrachenlance()).NaturalOrder(), sprint: true);
    }

    private static Vector2?[] MainTankStepsOutOfDrachenlance()
    {
        var spots = new Vector2?[8];
        spots[(int)PartyRole.MainTank] = SouthStack + new Vector2(2f, 0f);
        return spots;
    }

    private static Vector2?[] EveryoneNorthOfTheSouthDrachenlance()
    {
        var spots = Stacked(NorthStack);
        spots[(int)PartyRole.MainTank] = MainTankSpot;
        return spots;
    }

    private static Vector2?[] MainTankNorthPartySouth()
    {
        var spots = Enumerable.Range(0, 8).Select(i => (Vector2?)(SouthStack + new Vector2(0.6f * (i % 3 - 1), 0.6f * (i / 3 - 1)))).ToArray();
        spots[(int)PartyRole.MainTank] = MainTankSpot;
        return spots;
    }

    private Vector2?[] ClaimDiveSpotsByNumber()
    {
        var spots = new Vector2?[8];
        foreach (var (role, i) in state.Line(1).Select((r, i) => (r, i)))
            spots[(int)role] = Flat(AtBearing(OddDiveBearings[i], OddDiveRadius));
        foreach (var (role, i) in state.Line(3).Select((r, i) => (r, i)))
            spots[(int)role] = Flat(AtBearing(OddDiveBearings[i], ThirdsClaimRadius));
        foreach (var (role, i) in state.Line(2).Select((r, i) => (r, i)))
            spots[(int)role] = Flat(AtBearing(EvenDiveBearings[i], EvenDiveRadius));
        return spots;
    }

    private Vector2?[] DiveSpotsAndNorthStack(int line)
    {
        var spots = Stacked(NorthStack);
        foreach (var (role, spot) in OddDiveAssignments(line))
            spots[(int)role] = spot;
        return spots;
    }

    private Vector2?[] OddDiveSpots(int line)
    {
        var spots = new Vector2?[8];
        foreach (var (role, spot) in OddDiveAssignments(line))
            spots[(int)role] = spot;
        return spots;
    }

    private Vector2?[] EvenDiveSpots()
    {
        var spots = new Vector2?[8];
        foreach (var (role, spot) in EvenDiveAssignments())
            spots[(int)role] = spot;
        return spots;
    }

    private IEnumerable<(PartyRole Role, Vector2 Spot)> OddDiveAssignments(int line) =>
        OrderedDivers(line).Select((role, i) => (role, Flat(AtBearing(OddDiveBearings[i], OddDiveRadius))));

    private IEnumerable<(PartyRole Role, Vector2 Spot)> EvenDiveAssignments() =>
        OrderedDivers(2).Select((role, i) => (role, Flat(AtBearing(EvenDiveBearings[i], EvenDiveRadius))));

    private List<PartyRole> OrderedDivers(int line)
    {
        var divers = state.Line(line).ToList();
        if (!state.LineHasArrows(line)) return divers;
        var up = divers.First(r => state.Arrows[r] == DiveArrow.Up);
        var down = divers.First(r => state.Arrows[r] == DiveArrow.Down);
        var circles = divers.Where(r => state.Arrows[r] == DiveArrow.Circle).ToList();
        return line == 2 ? [up, down] : [up, circles[0], down];
    }

    private void FaceArrowsWest(int line)
    {
        foreach (var role in state.Line(line).Where(r => state.Arrows[r] != DiveArrow.Circle))
            if (world.Party.Get(role) is { } member && world.Party.IsBotDriven(member))
                member.SetRotation(FacingWest);
    }

    private IEnumerable<(PartyRole Role, Vector3 Spot)> TowerSoakers(int line)
    {
        var towers = state.DiveTowers[line - 1];
        if (towers.Count == 0) yield break;
        var byX = towers.OrderBy(t => t.Spot.X).ToList();
        var eastTower = byX[^1].Spot;
        var westTower = byX[0].Spot;
        switch (line)
        {
            case 1:
            {
                var soakers = OrderedDivers(3);
                var south = towers.OrderByDescending(t => t.Spot.Z).First().Spot;
                yield return (soakers[0], eastTower);
                yield return (soakers[1], south);
                yield return (soakers[2], westTower);
                break;
            }
            case 2:
            {
                var soakers = OrderedDivers(1);
                yield return (soakers[0], eastTower);
                yield return (soakers[2], westTower);
                break;
            }
            default:
            {
                var even = OrderedDivers(2);
                var south = towers.OrderByDescending(t => t.Spot.Z).First().Spot;
                yield return (even[0], eastTower);
                yield return (OrderedDivers(1)[1], south);
                yield return (even[1], westTower);
                break;
            }
        }
    }

    private Vector2?[] WaitOnTowerBearingsForFirstWheel(int wheel, int line)
    {
        var radius = state.LashFirst[wheel] ? InsideWheelRadius : OutsideWheelRadius;
        var spots = Stacked(new Vector2(0f, -radius));
        foreach (var (role, tower) in TowerSoakers(line))
            spots[(int)role] = Vector2.Normalize(Flat(tower)) * radius;
        return spots;
    }

    private Vector2?[] SoakDiveTowers(int line, int? wheel)
    {
        var secondWheelInside = wheel is { } w && !state.LashFirst[w];
        var others = wheel is null ? NorthStack : new Vector2(0f, secondWheelInside ? -InsideWheelRadius : -OutsideWheelRadius);
        var spots = wheel is null ? new Vector2?[8] : Stacked(others);
        foreach (var (role, tower) in TowerSoakers(line))
        {
            var at = Flat(tower);
            var outward = Vector2.Normalize(at);
            spots[(int)role] = wheel is null
                ? at + outward * 2f
                : secondWheelInside ? outward * MathF.Max(at.Length() - TowerEdge, 1.5f) : at + outward * TowerEdge;
        }
        return spots;
    }

    private Vector2?[] BaitGeirskoguls(int line)
    {
        var spots = new Vector2?[8];
        foreach (var (role, tower) in TowerSoakers(line))
            spots[(int)role] = Flat(tower) + Vector2.Normalize(Flat(tower)) * BaitBeyondTower;
        return spots;
    }

    private Vector2?[] BaitersToNorthStack(int line)
    {
        var spots = new Vector2?[8];
        var tight = Stacked(NorthStack);
        foreach (var (role, _) in TowerSoakers(line))
            spots[(int)role] = tight[(int)role];
        return spots;
    }

    private Vector2?[] EvenDiversToNorthStack()
    {
        var spots = new Vector2?[8];
        var tight = Stacked(NorthStack);
        foreach (var role in state.Line(2))
            spots[(int)role] = tight[(int)role];
        return spots;
    }

    private Vector2?[] EveryoneButMainTankNorthWest()
    {
        var spots = Stacked(NorthWestHold);
        spots[(int)PartyRole.MainTank] = MainTankSpot;
        return spots;
    }

    private Dictionary<PartyRole, int> FourTowerAssignments()
    {
        var needed = state.TowerSoakers.ToArray();
        var assigned = new Dictionary<PartyRole, int>();
        for (var i = 0; i < FourTowerPairs.Length; i++)
        {
            assigned[FourTowerPairs[i].Anchor] = i;
            needed[i]--;
        }
        for (var i = 0; i < FourTowerPairs.Length; i++)
        {
            var sameSide = i is 0 or 1 ? new[] { 0, 1 } : new[] { 2, 3 };
            var tower = needed[i] > 0 ? i : sameSide.Where(t => needed[t] > 0).Select(t => (int?)t).FirstOrDefault()
                ?? Enumerable.Range(0, needed.Length).Where(t => needed[t] > 0).Select(t => (int?)t).FirstOrDefault() ?? i;
            assigned[FourTowerPairs[i].Partner] = tower;
            needed[tower]--;
        }
        return assigned;
    }

    private Vector2?[] SoakFourTowers()
    {
        var spots = new Vector2?[8];
        foreach (var (role, tower) in FourTowerAssignments())
            spots[(int)role] = Flat(AtBearing(DsrP3NidhoggState.TowerBearings[tower], 11.31f)) + Spread(role);
        return spots;
    }

    private Vector2?[] BaitFourTowerGeirskoguls()
    {
        var spots = new Vector2?[8];
        foreach (var (role, tower) in FourTowerAssignments())
            spots[(int)role] = Flat(AtBearing(DsrP3NidhoggState.TowerBearings[tower], FourTowerBaitRadius)) + Spread(role);
        return spots;
    }

    private Vector2?[] DodgeToCardinalsAndInterceptTethers()
    {
        var spots = new Vector2?[8];
        foreach (var (role, tower) in FourTowerAssignments())
            spots[(int)role] = Flat(AtBearing(DsrP3NidhoggState.TowerBearings[tower] - 45f, FourTowerDodgeRadius)) + Spread(role);
        var tanks = new[] { PartyRole.MainTank, PartyRole.OffTank };
        for (var i = 0; i < tanks.Length; i++)
            if (spots[(int)state.SoulTetherTargets[i]] is { } holder)
                spots[(int)tanks[i]] = holder * TetherInterceptShare;
        return spots;
    }

    private static Vector2?[] TanksApartForSoulTethers()
    {
        var spots = new Vector2?[8];
        spots[(int)PartyRole.MainTank] = Vector2.Zero;
        spots[(int)PartyRole.OffTank] = new Vector2(0f, TankBusterOffTankRadius);
        return spots;
    }

    private static Vector2 Spread(PartyRole role) => new(0.5f * ((int)role % 3 - 1), 0.5f * ((int)role / 3 - 1));

    private static Vector2?[] Stacked(Vector2 centre) =>
        Enumerable.Range(0, 8).Select(i => (Vector2?)(centre + new Vector2(0.5f * (i % 3 - 1), 0.5f * (i / 3 - 1)))).ToArray();

    private static Vector2 Flat(Vector3 p) => new(p.X, p.Z);
}
